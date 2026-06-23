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

    init(fileURL: URL? = nil) {
        self.fileURL = fileURL ?? Self.makeStorageURL()
        self.load()
    }

    func add(_ item: LinkItem) {
        links.insert(item, at: 0)
        save()
    }

    func update(_ item: LinkItem) {
        guard let index = links.firstIndex(where: { $0.id == item.id }) else { return }
        links[index] = item
        save()
    }

    func delete(_ ids: Set<LinkItem.ID>) {
        links.removeAll { ids.contains($0.id) }
        save()
    }

    func markOpened(_ id: LinkItem.ID) {
        guard let index = links.firstIndex(where: { $0.id == id }) else { return }
        links[index].lastOpenedAt = Date()
        save()
    }

    func togglePinned(_ id: LinkItem.ID) {
        guard let index = links.firstIndex(where: { $0.id == id }) else { return }
        links[index].isPinned.toggle()
        save()
    }

    func replaceAll(_ next: [LinkItem]) {
        links = next
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
