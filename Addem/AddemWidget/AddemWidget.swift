import WidgetKit
import SwiftUI
import AppIntents

private enum WidgetPalette: String, CaseIterable {
    case purple
    case indigo
    case blue
    case teal
    case mint
    case green
    case gold
    case orange
    case coral
    case red
    case pink

    var tint: Color {
        switch self {
        case .purple: return Color(red: 0.67, green: 0.56, blue: 0.93)
        case .indigo: return Color(red: 0.48, green: 0.54, blue: 0.92)
        case .blue: return Color(red: 0.41, green: 0.66, blue: 0.96)
        case .teal: return Color(red: 0.33, green: 0.75, blue: 0.80)
        case .mint: return Color(red: 0.38, green: 0.83, blue: 0.70)
        case .green: return Color(red: 0.42, green: 0.78, blue: 0.51)
        case .gold: return Color(red: 0.90, green: 0.73, blue: 0.36)
        case .orange: return Color(red: 0.95, green: 0.60, blue: 0.33)
        case .coral: return Color(red: 0.95, green: 0.52, blue: 0.47)
        case .red: return Color(red: 0.90, green: 0.40, blue: 0.45)
        case .pink: return Color(red: 0.91, green: 0.49, blue: 0.72)
        }
    }

    var strong: Color {
        switch self {
        case .purple: return Color(red: 0.46, green: 0.33, blue: 0.83)
        case .indigo: return Color(red: 0.34, green: 0.39, blue: 0.78)
        case .blue: return Color(red: 0.29, green: 0.49, blue: 0.84)
        case .teal: return Color(red: 0.22, green: 0.57, blue: 0.64)
        case .mint: return Color(red: 0.25, green: 0.62, blue: 0.54)
        case .green: return Color(red: 0.29, green: 0.58, blue: 0.36)
        case .gold: return Color(red: 0.73, green: 0.52, blue: 0.22)
        case .orange: return Color(red: 0.78, green: 0.40, blue: 0.20)
        case .coral: return Color(red: 0.79, green: 0.33, blue: 0.28)
        case .red: return Color(red: 0.73, green: 0.25, blue: 0.31)
        case .pink: return Color(red: 0.74, green: 0.30, blue: 0.59)
        }
    }
}

private struct LiquidStyle {
    let accent: Color
    let accentSoft: Color
    let brandLead: Color
    let brandTrail: Color
    let brandEmTop: Color
    let brandEmBottom: Color
    let textPrimary: Color
    let textSecondary: Color
    let bullet: Color
    let rowBorder: Color
    let outline: Color
    let controlGlass: Color
    let glass1: Color
    let glass2: Color
    let glass3: Color
    let glow: Color

    init(palette: WidgetPalette, isDark: Bool) {
        if isDark {
            accent = palette.tint
            accentSoft = Color.white.opacity(0.90)
            brandLead = Color.white.opacity(0.94)
            brandTrail = palette.tint
            brandEmTop = Color.white
            brandEmBottom = palette.tint
            textPrimary = Color.white.opacity(0.95)
            textSecondary = Color.white.opacity(0.78)
            bullet = Color.white.opacity(0.92)
            rowBorder = Color.white.opacity(0.40)
            outline = Color.white.opacity(0.46)
            controlGlass = Color.white.opacity(0.22)
            glass1 = palette.tint.opacity(0.38)
            glass2 = palette.strong.opacity(0.46)
            glass3 = palette.tint.opacity(0.24)
            glow = palette.tint.opacity(0.22)
        } else {
            accent = palette.strong
            accentSoft = palette.tint
            brandLead = palette.strong
            brandTrail = palette.tint
            brandEmTop = Color.white
            brandEmBottom = palette.tint
            textPrimary = Color(red: 0.14, green: 0.11, blue: 0.24)
            textSecondary = Color(red: 0.30, green: 0.26, blue: 0.42)
            bullet = Color.white.opacity(0.98)
            rowBorder = Color.white.opacity(0.58)
            outline = Color.white.opacity(0.74)
            controlGlass = Color.white.opacity(0.34)
            glass1 = palette.tint.opacity(0.45)
            glass2 = palette.strong.opacity(0.28)
            glass3 = palette.tint.opacity(0.20)
            glow = palette.tint.opacity(0.26)
        }
    }
}

struct AddemEntry: TimelineEntry {
    let date: Date
    let links: [LinkItem]
    let paletteRaw: String
}

struct AddemProvider: TimelineProvider {
    func placeholder(in context: Context) -> AddemEntry {
        AddemEntry(date: Date(), links: sampleLinks, paletteRaw: currentPaletteRaw())
    }

    func getSnapshot(in context: Context, completion: @escaping (AddemEntry) -> Void) {
        let links = loadLinks()
        completion(AddemEntry(date: Date(), links: links.isEmpty ? sampleLinks : links, paletteRaw: currentPaletteRaw()))
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<AddemEntry>) -> Void) {
        let links = loadLinks()
        let entry = AddemEntry(date: Date(), links: links.isEmpty ? sampleLinks : links, paletteRaw: currentPaletteRaw())
        let refreshDate = Calendar.current.date(byAdding: .minute, value: 30, to: Date()) ?? Date().addingTimeInterval(1800)
        completion(Timeline(entries: [entry], policy: .after(refreshDate)))
    }

    private func currentPaletteRaw() -> String {
        let defaults = UserDefaults(suiteName: AppGroupConfig.groupID)
        return defaults?.string(forKey: AppGroupConfig.themePaletteKey) ?? WidgetPalette.purple.rawValue
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
    @Environment(\.colorScheme) private var colorScheme
    var entry: AddemProvider.Entry
    private var style: LiquidStyle {
        let palette = WidgetPalette(rawValue: entry.paletteRaw) ?? .purple
        return LiquidStyle(palette: palette, isDark: colorScheme == .dark)
    }

    var body: some View {
        ZStack(alignment: .topLeading) {
            glassBackground

            switch family {
            case .systemSmall:
                smallLayout
            default:
                mediumLayout
            }
        }
    }

    private var smallLayout: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack(spacing: 6) {
                brandTitle(icon: "link.badge.plus", size: 17)
                Spacer()
                Button(intent: OpenQuickAddIntent()) {
                    Image(systemName: "plus")
                        .font(.system(size: 12, weight: .bold))
                        .frame(width: 24, height: 24)
                        .background(style.controlGlass)
                        .clipShape(Circle())
                }
                .buttonStyle(.plain)
            }

            if entry.links.isEmpty {
                Text("Paste a link in AddEm")
                    .font(.system(size: 13, weight: .medium))
                    .foregroundStyle(style.textSecondary)
                    .lineLimit(2)
            } else {
                ForEach(entry.links.prefix(2)) { link in
                    compactRow(link)
                }
            }

            Spacer(minLength: 0)
        }
        .padding(12)
    }

    private var mediumLayout: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack(alignment: .center, spacing: 6) {
                brandTitle(icon: "sparkles", size: 20)
                Spacer()
                Text("\(entry.links.count) links")
                    .font(.system(size: 11, weight: .semibold))
                    .padding(.horizontal, 8)
                    .padding(.vertical, 4)
                    .background(.ultraThinMaterial)
                    .overlay(
                        Capsule()
                            .stroke(Color.white.opacity(0.52), lineWidth: 0.8)
                    )
                    .clipShape(Capsule())

                Button(intent: OpenQuickAddIntent()) {
                    Image(systemName: "plus")
                        .font(.system(size: 12, weight: .bold))
                        .frame(width: 24, height: 24)
                        .background(style.controlGlass)
                        .clipShape(Circle())
                }
                .buttonStyle(.plain)

                Button(intent: OpenTopLinkIntent()) {
                    Image(systemName: "safari")
                        .font(.system(size: 11, weight: .bold))
                        .frame(width: 24, height: 24)
                        .background(style.controlGlass)
                        .clipShape(Circle())
                }
                .buttonStyle(.plain)
            }

            if entry.links.isEmpty {
                Text("No links yet. Add one in the app and pin it.")
                    .font(.system(size: 13, weight: .medium))
                    .foregroundStyle(style.textSecondary)
                    .lineLimit(2)
            } else {
                ForEach(entry.links.prefix(4)) { link in
                    expandedRow(link)
                }
            }

            Spacer(minLength: 0)
        }
        .padding(12)
    }

    private func brandTitle(icon: String, size: CGFloat) -> some View {
        HStack(spacing: 3) {
            Image(systemName: icon)
                .font(.system(size: size * 0.9, weight: .bold))
                .foregroundStyle(style.accent)
            HStack(spacing: 0) {
                Text("Add")
                    .foregroundStyle(
                        LinearGradient(
                            colors: [style.brandLead, style.brandTrail],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        )
                    )
                Text("Em")
                    .foregroundStyle(
                        LinearGradient(
                            colors: [style.brandEmTop, style.brandEmBottom],
                            startPoint: .top,
                            endPoint: .bottom
                        )
                    )
            }
            .font(.system(size: size, weight: .black, design: .rounded))
        }
        .accessibilityLabel("AddEm")
    }

    private func compactRow(_ link: LinkItem) -> some View {
        HStack(spacing: 8) {
            Group {
                if let url = link.url {
                    Link(destination: url) {
                        HStack(spacing: 8) {
                            Circle()
                                .fill(style.bullet)
                                .frame(width: 7, height: 7)
                            Text(link.title)
                                .font(.system(size: 13, weight: .semibold))
                                .foregroundStyle(style.textPrimary)
                                .lineLimit(1)
                            Spacer(minLength: 0)
                        }
                        .foregroundStyle(.primary)
                    }
                } else {
                    HStack(spacing: 8) {
                        Text(link.title)
                            .font(.system(size: 13, weight: .semibold))
                            .foregroundStyle(style.textPrimary)
                            .lineLimit(1)
                        Spacer(minLength: 0)
                    }
                }
            }

            pinToggleButton(link)
        }
        .padding(.horizontal, 8)
        .padding(.vertical, 6)
        .background(
            LinearGradient(
                colors: [Color.white.opacity(0.40), Color.white.opacity(0.24)],
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
        )
        .background(.ultraThinMaterial)
        .overlay(
            RoundedRectangle(cornerRadius: 9, style: .continuous)
                .stroke(style.rowBorder, lineWidth: 0.9)
        )
        .clipShape(RoundedRectangle(cornerRadius: 9, style: .continuous))
        .foregroundStyle(.primary)
    }

    private func pinToggleButton(_ link: LinkItem) -> some View {
        Button(intent: TogglePinIntent(linkID: link.id.uuidString)) {
            Image(systemName: link.isPinned ? "pin.slash.fill" : "pin.fill")
                .font(.system(size: 11, weight: .bold))
                .foregroundStyle(style.textSecondary)
                .frame(width: 22, height: 22)
                .background(style.controlGlass)
                .clipShape(Circle())
        }
        .buttonStyle(.plain)
    }

    private func expandedRow(_ link: LinkItem) -> some View {
        HStack(spacing: 8) {
            Group {
                if let url = link.url {
                    Link(destination: url) {
                        HStack(spacing: 10) {
                            Circle()
                                .fill(style.bullet)
                                .frame(width: 8, height: 8)
                            VStack(alignment: .leading, spacing: 1) {
                                Text(link.title)
                                    .font(.system(size: 14, weight: .semibold))
                                    .foregroundStyle(style.textPrimary)
                                    .lineLimit(1)
                                Text(link.domain)
                                    .font(.system(size: 11, weight: .medium))
                                    .foregroundStyle(style.textSecondary)
                                    .lineLimit(1)
                            }
                            Spacer(minLength: 0)
                        }
                        .foregroundStyle(.primary)
                    }
                } else {
                    HStack(spacing: 10) {
                        Text(link.title)
                            .font(.system(size: 14, weight: .semibold))
                            .foregroundStyle(style.textPrimary)
                            .lineLimit(1)
                        Spacer(minLength: 0)
                    }
                }
            }

            pinToggleButton(link)
        }
        .padding(.horizontal, 10)
        .padding(.vertical, 7)
        .background(
            LinearGradient(
                colors: [Color.white.opacity(0.40), Color.white.opacity(0.22)],
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
        )
        .background(.ultraThinMaterial)
        .overlay(
            RoundedRectangle(cornerRadius: 10, style: .continuous)
                .stroke(style.rowBorder, lineWidth: 1)
        )
        .clipShape(RoundedRectangle(cornerRadius: 10, style: .continuous))
        .foregroundStyle(.primary)
    }

    private var glassBackground: some View {
        ZStack {
            RoundedRectangle(cornerRadius: 24, style: .continuous)
                .fill(.ultraThinMaterial)

            RoundedRectangle(cornerRadius: 24, style: .continuous)
                .fill(
                    LinearGradient(
                        colors: [
                            style.glass1,
                            style.glass2,
                            style.glass3
                        ],
                        startPoint: .topLeading,
                        endPoint: .bottomTrailing
                    )
                )

            RoundedRectangle(cornerRadius: 24, style: .continuous)
                .fill(
                    LinearGradient(
                        colors: [
                            Color.white.opacity(0.34),
                            Color.white.opacity(0.08),
                            Color.black.opacity(0.06)
                        ],
                        startPoint: .topLeading,
                        endPoint: .bottomTrailing
                    )
                )

            Circle()
                .fill(Color.white.opacity(0.34))
                .frame(width: 170, height: 170)
                .blur(radius: 20)
                .offset(x: -96, y: -95)

            Circle()
                .fill(style.glow)
                .frame(width: 190, height: 190)
                .blur(radius: 28)
                .offset(x: 110, y: 66)

            RoundedRectangle(cornerRadius: 24, style: .continuous)
                .stroke(style.outline, lineWidth: 1)
        }
    }
}

struct AddemWidget: Widget {
    private let kind = "AddEmWidget"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: AddemProvider()) { entry in
            AddemWidgetView(entry: entry)
                .containerBackground(for: .widget) {
                    Color.clear
                }
        }
        .configurationDisplayName("AddEm Links")
        .description("Quick access to your pinned or recent links.")
        .supportedFamilies([.systemSmall, .systemMedium])
        .contentMarginsDisabled()
    }
}

@main
struct AddemWidgetBundle: WidgetBundle {
    var body: some Widget {
        AddemWidget()
    }
}
