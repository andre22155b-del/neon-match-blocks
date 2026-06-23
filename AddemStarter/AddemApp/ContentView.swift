import SwiftUI
import AppKit

struct ContentView: View {
    @EnvironmentObject private var store: LinkStore

    @State private var searchText = ""
    @State private var selectedTag: String? = nil
    @State private var showAddSheet = false

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
            LinearGradient(
                colors: [
                    AddemTheme.panelTop.opacity(0.55),
                    AddemTheme.panelBottom.opacity(0.42),
                    Color.white.opacity(0.55)
                ],
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
            .ignoresSafeArea()

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
    }

    private var header: some View {
        HStack(spacing: 10) {
            Text("Addem")
                .font(.system(size: 26, weight: .bold, design: .rounded))
                .foregroundStyle(AddemTheme.purpleTint)

            TextField("Search links, notes, tags...", text: $searchText)
                .textFieldStyle(.roundedBorder)

            Button {
                showAddSheet = true
            } label: {
                Label("Add", systemImage: "plus")
            }
            .buttonStyle(.borderedProminent)
            .tint(AddemTheme.purpleTint)
        }
        .glassCard()
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
                        Text(link.urlString)
                            .font(.caption)
                            .foregroundStyle(.secondary)
                        if !link.notes.isEmpty {
                            Text(link.notes)
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }
                        HStack(spacing: 6) {
                            ForEach(link.tags, id: \.self) { tag in
                                Text(tag)
                                    .font(.caption2)
                                    .padding(.horizontal, 8)
                                    .padding(.vertical, 4)
                                    .background(AddemTheme.purpleTint.opacity(0.15))
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
                    }
                }
                .padding(.vertical, 6)
            }
            .onDelete { offsets in
                let ids = Set(offsets.map { filteredLinks[$0].id })
                store.delete(ids)
            }
        }
        .scrollContentBackground(.hidden)
        .background(Color.clear)
        .glassCard()
    }

    private func tagChip(_ title: String, isActive: Bool, action: @escaping () -> Void) -> some View {
        Button(action: action) {
            Text(title)
                .font(.caption)
                .padding(.horizontal, 10)
                .padding(.vertical, 6)
                .background(isActive ? AddemTheme.purpleTint.opacity(0.26) : Color.white.opacity(0.55))
                .clipShape(Capsule())
        }
        .buttonStyle(.plain)
    }

    private func open(_ link: LinkItem) {
        guard let url = link.url else { return }
        NSWorkspace.shared.open(url)
        store.markOpened(link.id)
    }
}
