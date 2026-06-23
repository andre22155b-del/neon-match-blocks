import WidgetKit
import SwiftUI

struct AddemEntry: TimelineEntry {
    let date: Date
    let links: [LinkItem]
}

struct AddemProvider: TimelineProvider {
    func placeholder(in context: Context) -> AddemEntry {
        AddemEntry(date: Date(), links: sampleLinks)
    }

    func getSnapshot(in context: Context, completion: @escaping (AddemEntry) -> Void) {
        let links = loadLinks()
        completion(AddemEntry(date: Date(), links: links.isEmpty ? sampleLinks : links))
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<AddemEntry>) -> Void) {
        let links = loadLinks()
        let entry = AddemEntry(date: Date(), links: links.isEmpty ? sampleLinks : links)
        let refreshDate = Calendar.current.date(byAdding: .minute, value: 30, to: Date()) ?? Date().addingTimeInterval(1800)
        completion(Timeline(entries: [entry], policy: .after(refreshDate)))
    }

    private func loadLinks() -> [LinkItem] {
        let all = LinkStoreReader.load()
        let pinned = all.filter { $0.isPinned }
        if !pinned.isEmpty {
            return pinned.sorted { $0.createdAt > $1.createdAt }
        }
        return all.sorted { $0.createdAt > $1.createdAt }
    }

    private var sampleLinks: [LinkItem] {
        [
            LinkItem(urlString: "https://developer.apple.com", title: "Apple Dev", tags: ["dev", "docs"], notes: "SwiftUI docs", isPinned: true),
            LinkItem(urlString: "https://news.ycombinator.com", title: "HN", tags: ["tech"], notes: "Morning browse", isPinned: true)
        ]
    }
}

struct AddemWidgetView: View {
    @Environment(\.widgetFamily) private var family
    var entry: AddemProvider.Entry

    var body: some View {
        ZStack {
            LinearGradient(
                colors: [
                    Color(red: 0.95, green: 0.90, blue: 1.0).opacity(0.88),
                    Color(red: 0.88, green: 0.82, blue: 0.98).opacity(0.72)
                ],
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
            .background(.ultraThinMaterial)

            VStack(alignment: .leading, spacing: 8) {
                Text("Addem")
                    .font(.headline)
                    .foregroundStyle(Color(red: 0.39, green: 0.28, blue: 0.67))

                switch family {
                case .systemSmall:
                    ForEach(entry.links.prefix(2)) { link in
                        widgetRow(link)
                    }
                default:
                    ForEach(entry.links.prefix(4)) { link in
                        widgetRow(link)
                    }
                }

                if entry.links.isEmpty {
                    Text("No links yet")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
            .padding(12)
        }
        .clipShape(RoundedRectangle(cornerRadius: 18, style: .continuous))
    }

    private func widgetRow(_ link: LinkItem) -> some View {
        Group {
            if let url = link.url {
                Link(destination: url) {
                    HStack(spacing: 6) {
                        Circle()
                            .fill(Color.white.opacity(0.8))
                            .frame(width: 6, height: 6)
                        Text(link.title)
                            .font(.caption)
                            .lineLimit(1)
                    }
                    .foregroundStyle(.primary)
                }
            } else {
                Text(link.title)
                    .font(.caption)
                    .lineLimit(1)
            }
        }
    }
}

struct AddemWidget: Widget {
    private let kind = "AddemWidget"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: AddemProvider()) { entry in
            AddemWidgetView(entry: entry)
        }
        .configurationDisplayName("Addem Links")
        .description("Quick access to your pinned or recent links.")
        .supportedFamilies([.systemSmall, .systemMedium])
    }
}

@main
struct AddemWidgetBundle: WidgetBundle {
    var body: some Widget {
        AddemWidget()
    }
}
