import SwiftUI
import AppKit
import WidgetKit

struct ContentView: View {
    @EnvironmentObject private var store: LinkStore
    @Environment(\.scenePhase) private var scenePhase
    @Environment(\.colorScheme) private var colorScheme

    @State private var searchText = ""
    @State private var selectedTag: String? = nil
    @State private var showAddSheet = false
    @State private var editingLink: LinkItem? = nil
    @State private var pendingDelete: LinkItem? = nil
    @State private var recentlyDeleted: LinkItem? = nil
    @State private var undoClearTask: Task<Void, Never>? = nil
    @State private var searchTrackTask: Task<Void, Never>? = nil
    @State private var lastTrackedSearchQuery = ""
    @AppStorage(AddemTheme.paletteKey, store: UserDefaults(suiteName: AppGroupConfig.groupID))
    private var themeID = AddemPalette.purple.rawValue

    private var allTags: [String] {
        let tags = store.links.flatMap(\.tags)
        return Array(Set(tags)).sorted()
    }

    private var filteredLinks: [LinkItem] {
        store.links.filter { link in
            let matchesText = searchText.isEmpty || [link.title, link.urlString, link.notes, link.tags.joined(separator: " ")]
                .joined(separator: " ")
                .localizedCaseInsensitiveContains(searchText)

            let matchesTag = selectedTag == nil || link.tags.contains(selectedTag!)
            return matchesText && matchesTag
        }
    }

    var body: some View {
        ZStack {
            AddemLiquidBackground()

            VStack(spacing: 14) {
                header
                tagFilter
                linksList
            }
            .padding(18)
        }
        .sheet(isPresented: $showAddSheet) {
            AddLinkSheet()
                .environmentObject(store)
                .frame(width: 560, height: 500)
        }
        .sheet(item: $editingLink) { link in
            EditLinkSheet(link: link)
                .environmentObject(store)
                .frame(width: 560, height: 420)
        }
        .alert(
            "Remove this link?",
            isPresented: Binding(
                get: { pendingDelete != nil },
                set: { isPresented in
                    if !isPresented { pendingDelete = nil }
                }
            ),
            presenting: pendingDelete
        ) { link in
            Button("Cancel", role: .cancel) {}
            Button("Remove", role: .destructive) {
                remove(link)
                pendingDelete = nil
            }
        } message: { link in
            Text("This will delete \"\(link.title)\".")
        }
        .overlay(alignment: .bottom) {
            if let recentlyDeleted {
                HStack(spacing: 10) {
                    Image(systemName: "trash")
                        .foregroundStyle(AddemTheme.textSecondary)
                    Text("Removed \(recentlyDeleted.title)")
                        .font(.subheadline.weight(.semibold))
                        .foregroundStyle(AddemTheme.textPrimary)
                        .lineLimit(1)
                    Spacer()
                    Button("Undo") {
                        undoDelete()
                    }
                    .buttonStyle(.borderedProminent)
                    .tint(AddemTheme.purpleStrong)
                }
                .padding(.horizontal, 6)
                .padding(.vertical, 2)
                .glassCard()
                .padding(.horizontal, 18)
                .padding(.bottom, 14)
                .transition(.move(edge: .bottom).combined(with: .opacity))
            }
        }
        .animation(.easeInOut(duration: 0.2), value: recentlyDeleted != nil)
        .onDisappear {
            undoClearTask?.cancel()
            searchTrackTask?.cancel()
        }
        .onAppear {
            consumeWidgetLaunchActions()
        }
        .onReceive(NotificationCenter.default.publisher(for: .addemOpenQuickAdd)) { _ in
            showAddSheet = true
        }
        .onChange(of: scenePhase) { _, phase in
            if phase == .active {
                consumeWidgetLaunchActions()
            }
        }
        .onChange(of: themeID) { _, _ in
            WidgetCenter.shared.reloadAllTimelines()
        }
        .onChange(of: searchText) { _, newValue in
            scheduleSearchTracking(for: newValue)
        }
    }

    private var header: some View {
        HStack(spacing: 8) {
            brandWordmark

            TextField("Search links, notes, tags...", text: $searchText)
                .textFieldStyle(.plain)
                .padding(.horizontal, 12)
                .padding(.vertical, 8)
                .glassInputField(cornerRadius: 12)

            Button {
                showAddSheet = true
            } label: {
                Label("Add", systemImage: "plus")
            }
            .buttonStyle(.borderedProminent)
            .tint(AddemTheme.purpleTint)
            .keyboardShortcut("a", modifiers: [.command, .shift])

            Menu {
                ForEach(AddemPalette.allCases) { palette in
                    Button {
                        themeID = palette.rawValue
                    } label: {
                        HStack {
                            Circle()
                                .fill(palette.tint)
                                .frame(width: 10, height: 10)
                            Text(palette.title)
                            if themeID == palette.rawValue {
                                Image(systemName: "checkmark")
                            }
                        }
                    }
                }
            } label: {
                Label("Theme", systemImage: "paintpalette")
            }
            .menuStyle(.borderlessButton)
        }
        .glassCard()
    }

    private var brandWordmark: some View {
        let titleLead = colorScheme == .dark ? Color.white.opacity(0.95) : AddemTheme.purpleStrong
        let titleTrail = colorScheme == .dark ? AddemTheme.purpleTint : AddemTheme.purpleTint.opacity(0.95)
        let plateTop = colorScheme == .dark ? Color.white.opacity(0.26) : AddemTheme.purpleTint.opacity(0.30)
        let plateMid = colorScheme == .dark ? AddemTheme.purpleStrong.opacity(0.20) : AddemTheme.purpleStrong.opacity(0.16)
        let plateBottom = colorScheme == .dark ? Color.black.opacity(0.08) : Color.white.opacity(0.24)
        let border = colorScheme == .dark ? Color.white.opacity(0.56) : Color.white.opacity(0.72)

        return Text("AddEm")
            .foregroundStyle(
                LinearGradient(
                    colors: [titleLead, titleTrail],
                    startPoint: .topLeading,
                    endPoint: .bottomTrailing
                )
            )
        .font(.system(size: 26, weight: .black, design: .rounded))
        .padding(.horizontal, 12)
        .padding(.vertical, 6)
        .background(
            Capsule(style: .continuous)
                .fill(
                    LinearGradient(
                        colors: [plateTop, plateMid, plateBottom],
                        startPoint: .topLeading,
                        endPoint: .bottomTrailing
                    )
                )
        )
        .overlay(
            Capsule(style: .continuous)
                .stroke(border, lineWidth: 1)
        )
        .shadow(color: .black.opacity(0.10), radius: 6, x: 0, y: 3)
        .accessibilityLabel("AddEm")
    }

    private var tagFilter: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                tagChip("All", isActive: selectedTag == nil) { selectedTag = nil }
                ForEach(allTags, id: \.self) { tag in
                    tagChip(tag, isActive: selectedTag == tag) { selectedTag = tag }
                }
            }
            .padding(.horizontal, 4)
        }
    }

    private var linksList: some View {
        List {
            ForEach(filteredLinks) { link in
                HStack(alignment: .top, spacing: 12) {
                    VStack(alignment: .leading, spacing: 4) {
                        Text(link.title)
                            .font(.headline)
                            .foregroundStyle(AddemTheme.textPrimary)
                        if link.url != nil {
                            Button {
                                open(link)
                            } label: {
                                Text(link.urlString)
                                    .font(.caption)
                                    .foregroundStyle(AddemTheme.purpleStrong)
                                    .underline()
                                    .lineLimit(1)
                                    .truncationMode(.middle)
                            }
                            .buttonStyle(.plain)
                            .help("Open link")
                        } else {
                            Text(link.urlString)
                                .font(.caption)
                                .foregroundStyle(AddemTheme.textSecondary)
                                .lineLimit(1)
                                .truncationMode(.middle)
                        }
                        if !link.notes.isEmpty {
                            Text(link.notes)
                                .font(.caption)
                                .foregroundStyle(AddemTheme.textSecondary)
                        } else if let aiSummary = link.aiSummary, !aiSummary.isEmpty {
                            Text(aiSummary)
                                .font(.caption2)
                                .italic()
                                .foregroundStyle(AddemTheme.textSecondary.opacity(0.9))
                        }
                        HStack(spacing: 6) {
                            ForEach(link.tags, id: \.self) { tag in
                                Text(tag)
                                    .font(.caption2)
                                    .foregroundStyle(AddemTheme.textPrimary)
                                    .padding(.horizontal, 8)
                                    .padding(.vertical, 4)
                                    .background(AddemTheme.purpleTint.opacity(0.24))
                                    .clipShape(Capsule())
                            }
                        }
                    }

                    Spacer(minLength: 12)

                    VStack(alignment: .trailing, spacing: 8) {
                        Button(link.isPinned ? "Unpin" : "Pin") {
                            store.togglePinned(link.id)
                        }
                        .buttonStyle(.bordered)

                        Button("Open") {
                            open(link)
                        }
                        .buttonStyle(.borderedProminent)
                        .tint(AddemTheme.purpleTint)

                        Button("Edit") {
                            editingLink = link
                        }
                        .buttonStyle(.bordered)

                        Button("Remove", role: .destructive) {
                            pendingDelete = link
                        }
                        .buttonStyle(.bordered)
                    }
                }
                .padding(10)
                .background(rowGlass(isPinned: link.isPinned))
                .contextMenu {
                    Button("Edit") {
                        editingLink = link
                    }
                    Button(link.isPinned ? "Unpin" : "Pin") {
                        store.togglePinned(link.id)
                    }
                    Button(role: .destructive) {
                        pendingDelete = link
                    } label: {
                        Text("Remove")
                    }
                }
                .listRowBackground(Color.clear)
                .listRowSeparator(.hidden)
            }
        }
        .listStyle(.plain)
        .scrollContentBackground(.hidden)
        .background(Color.clear)
        .glassCard()
    }

    private func tagChip(_ title: String, isActive: Bool, action: @escaping () -> Void) -> some View {
        Button(action: action) {
            Text(title)
                .font(.caption)
                .foregroundStyle(AddemTheme.textPrimary)
                .padding(.horizontal, 10)
                .padding(.vertical, 6)
                .background(AddemTheme.chipFill(isActive: isActive, for: colorScheme))
                .clipShape(Capsule())
        }
        .buttonStyle(.plain)
    }

    private func rowGlass(isPinned: Bool) -> some View {
        let shape = RoundedRectangle(cornerRadius: 14, style: .continuous)
        let emphasis = isPinned ? 1.0 : 0.92

        return shape
            .fill(AddemTheme.glassBaseGradient(for: colorScheme).opacity(emphasis))
            .overlay(shape.fill(.ultraThinMaterial).opacity(0.88))
            .overlay(shape.stroke(AddemTheme.glassBorder(for: colorScheme).opacity(0.90), lineWidth: 1))
            .shadow(color: AddemTheme.surfaceGlow(for: colorScheme).opacity(0.85), radius: 12, x: 0, y: 4)
    }

    private func open(_ link: LinkItem) {
        guard let url = link.url else { return }
        NSWorkspace.shared.open(url)
        store.markOpened(link.id)
    }

    private func remove(_ link: LinkItem) {
        store.delete([link.id])
        recentlyDeleted = link
        undoClearTask?.cancel()
        undoClearTask = Task {
            try? await Task.sleep(nanoseconds: 8_000_000_000)
            if !Task.isCancelled {
                await MainActor.run {
                    recentlyDeleted = nil
                }
            }
        }
    }

    private func undoDelete() {
        guard let item = recentlyDeleted else { return }
        store.add(item, enrichWithAI: false, trackEvent: false)
        undoClearTask?.cancel()
        recentlyDeleted = nil
    }

    private func scheduleSearchTracking(for rawValue: String) {
        searchTrackTask?.cancel()

        let query = rawValue
            .trimmingCharacters(in: .whitespacesAndNewlines)
            .lowercased()

        guard !query.isEmpty else {
            lastTrackedSearchQuery = ""
            return
        }

        searchTrackTask = Task {
            try? await Task.sleep(nanoseconds: 800_000_000)
            guard !Task.isCancelled else { return }

            let latestQuery = searchText
                .trimmingCharacters(in: .whitespacesAndNewlines)
                .lowercased()

            guard !latestQuery.isEmpty else { return }
            guard latestQuery != lastTrackedSearchQuery else { return }

            lastTrackedSearchQuery = latestQuery
            AddemAnalytics.shared.track(
                .searchUsed,
                metadata: [
                    "length": "\(latestQuery.count)",
                    "tag_filter": selectedTag ?? "all"
                ]
            )
        }
    }

    private func consumeWidgetLaunchActions() {
        guard let defaults = UserDefaults(suiteName: AppGroupConfig.groupID) else { return }

        if defaults.bool(forKey: AppGroupConfig.quickAddFlagKey) {
            defaults.set(false, forKey: AppGroupConfig.quickAddFlagKey)
            showAddSheet = true
        }

        guard let rawURL = defaults.string(forKey: AppGroupConfig.quickOpenURLKey), !rawURL.isEmpty else {
            return
        }
        defaults.removeObject(forKey: AppGroupConfig.quickOpenURLKey)

        if let existing = store.links.first(where: { $0.urlString == rawURL }) {
            open(existing)
            return
        }

        if let url = URL(string: rawURL), url.scheme != nil {
            NSWorkspace.shared.open(url)
        }
    }
}
