import Foundation

#if canImport(Combine)
import Combine
#endif

#if canImport(SwiftUI)
import SwiftUI
#endif

#if canImport(WidgetKit)
import WidgetKit
#endif

@MainActor
final class LinkStore: ObservableObject {
    @Published private(set) var links: [LinkItem] = []

    private let fileURL: URL
    private let aiService: any AIService
    private var inFlightAI = Set<UUID>()

    init(fileURL: URL? = nil, aiService: any AIService = CompositeAIService()) {
        self.fileURL = fileURL ?? Self.makeStorageURL()
        self.aiService = aiService
        self.load()
    }

    func add(_ item: LinkItem, enrichWithAI: Bool = true, trackEvent: Bool = true) {
        links.insert(item, at: 0)
        save()
        if trackEvent {
            AddemAnalytics.shared.track(
                .linkSaved,
                metadata: [
                    "pinned": item.isPinned ? "1" : "0",
                    "tag_count": "\(item.tags.count)"
                ]
            )
        }
        if enrichWithAI {
            scheduleAIEnrichment(for: item)
        }
    }

    func update(_ item: LinkItem) {
        guard let index = links.firstIndex(where: { $0.id == item.id }) else { return }
        links[index] = item
        save()
    }

    func delete(_ ids: Set<LinkItem.ID>) {
        let previousCount = links.count
        links.removeAll { ids.contains($0.id) }
        save()
        let removedCount = previousCount - links.count
        if removedCount > 0 {
            AddemAnalytics.shared.track(.linkRemoved, metadata: ["count": "\(removedCount)"])
        }
    }

    func markOpened(_ id: LinkItem.ID) {
        guard let index = links.firstIndex(where: { $0.id == id }) else { return }
        links[index].lastOpenedAt = Date()
        save()
        AddemAnalytics.shared.track(
            .linkOpened,
            metadata: [
                "pinned": links[index].isPinned ? "1" : "0"
            ]
        )
    }

    func togglePinned(_ id: LinkItem.ID) {
        guard let index = links.firstIndex(where: { $0.id == id }) else { return }
        links[index].isPinned.toggle()
        save()
        if links[index].isPinned {
            AddemAnalytics.shared.track(.linkPinned)
        }
    }

    func replaceAll(_ next: [LinkItem]) {
        links = next
        save()
    }

    private func scheduleAIEnrichment(for item: LinkItem) {
        guard inFlightAI.insert(item.id).inserted else { return }

        Task { [weak self] in
            guard let self else { return }
            let result = await aiService.enrich(item: item)
            applyAIEnrichment(result, to: item)
            inFlightAI.remove(item.id)
        }
    }

    private func applyAIEnrichment(_ result: AIEnrichmentResult?, to baseline: LinkItem) {
        guard let result else { return }
        guard let index = links.firstIndex(where: { $0.id == baseline.id }) else { return }

        var item = links[index]
        var didChange = false

        if
            let suggested = result.title?.trimmingCharacters(in: .whitespacesAndNewlines),
            !suggested.isEmpty,
            SmartOrganizer.shouldReplaceTitle(currentTitle: item.title, urlString: item.urlString),
            item.title != suggested
        {
            item.title = suggested
            didChange = true
        }

        let normalizedTags = result.tags
            .map { $0.lowercased().trimmingCharacters(in: .whitespacesAndNewlines) }
            .filter { !$0.isEmpty }
        let mergedTags = Array(Set(item.tags + normalizedTags)).sorted()
        if mergedTags != item.tags {
            item.tags = mergedTags
            didChange = true
        }

        if let summary = result.summary?.trimmingCharacters(in: .whitespacesAndNewlines), !summary.isEmpty {
            if item.aiSummary != summary {
                item.aiSummary = summary
                didChange = true
            }
        }

        if item.aiConfidence != result.confidence {
            item.aiConfidence = result.confidence
            didChange = true
        }

        item.aiUpdatedAt = Date()
        if links[index].aiUpdatedAt != item.aiUpdatedAt {
            didChange = true
        }

        guard didChange else { return }
        links[index] = item
        save()
    }

    func load() {
        do {
            guard FileManager.default.fileExists(atPath: fileURL.path) else {
                links = []
                return
            }
            let data = try Data(contentsOf: fileURL)
            let decoder = JSONDecoder()
            decoder.dateDecodingStrategy = .iso8601
            links = try decoder.decode([LinkItem].self, from: data)
        } catch {
            links = []
            print("[LinkStore] load failed: \(error)")
        }
    }

    func save() {
        do {
            let folder = fileURL.deletingLastPathComponent()
            try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
            let encoder = JSONEncoder()
            encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
            encoder.dateEncodingStrategy = .iso8601
            let data = try encoder.encode(links)
            try data.write(to: fileURL, options: .atomic)
            #if canImport(WidgetKit)
            WidgetCenter.shared.reloadAllTimelines()
            #endif
        } catch {
            print("[LinkStore] save failed: \(error)")
        }
    }

    nonisolated static func makeStorageURL() -> URL {
        if let shared = FileManager.default.containerURL(
            forSecurityApplicationGroupIdentifier: AppGroupConfig.groupID
        ) {
            return shared.appendingPathComponent(AppGroupConfig.storageFileName)
        }

        // Fallback for preview/development without App Group configured.
        let support = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
        return support.appendingPathComponent("AddEm").appendingPathComponent(AppGroupConfig.storageFileName)
    }
}

struct LinkStoreReader {
    static func load() -> [LinkItem] {
        let url = LinkStore.makeStorageURL()
        do {
            guard FileManager.default.fileExists(atPath: url.path) else { return [] }
            let data = try Data(contentsOf: url)
            let decoder = JSONDecoder()
            decoder.dateDecodingStrategy = .iso8601
            return try decoder.decode([LinkItem].self, from: data)
        } catch {
            print("[LinkStoreReader] load failed: \(error)")
            return []
        }
    }
}
