import SwiftUI

struct MenuBarAddView: View {
    @EnvironmentObject private var store: LinkStore

    @State private var text = ""

    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("Addem Quick Capture")
                .font(.headline)

            TextField("Paste URL + optional notes", text: $text, axis: .vertical)
                .lineLimit(3)
                .textFieldStyle(.roundedBorder)

            Button("Save") {
                let draft = SmartOrganizer.draft(from: text)
                guard let url = URL(string: draft.urlString), url.scheme != nil else { return }
                let item = LinkItem(
                    urlString: url.absoluteString,
                    title: draft.title,
                    tags: draft.tags,
                    notes: draft.notes,
                    isPinned: false
                )
                store.add(item)
                text = ""
            }
            .buttonStyle(.borderedProminent)
            .tint(AddemTheme.purpleTint)
        }
        .padding(12)
        .frame(width: 300)
    }
}
