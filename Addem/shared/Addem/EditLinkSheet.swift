import SwiftUI

struct EditLinkSheet: View {
    @Environment(\.dismiss) private var dismiss
    @EnvironmentObject private var store: LinkStore

    private let original: LinkItem

    @State private var urlString: String
    @State private var title: String
    @State private var tagsCSV: String
    @State private var notes: String
    @State private var isPinned: Bool
    @State private var validationError = ""

    init(link: LinkItem) {
        self.original = link
        _urlString = State(initialValue: link.urlString)
        _title = State(initialValue: link.title)
        _tagsCSV = State(initialValue: link.tags.joined(separator: ", "))
        _notes = State(initialValue: link.notes)
        _isPinned = State(initialValue: link.isPinned)
    }

    var body: some View {
        ZStack {
            AddemLiquidBackground()

            VStack(alignment: .leading, spacing: 12) {
                Text("Edit Link")
                    .font(.title2.bold())
                    .foregroundStyle(AddemTheme.textPrimary)

                Group {
                    TextField("URL", text: $urlString)
                    TextField("Title", text: $title)
                    TextField("Tags (comma separated)", text: $tagsCSV)
                    TextField("Notes", text: $notes)
                    Toggle("Pinned", isOn: $isPinned)
                }
                .textFieldStyle(.roundedBorder)

                if !validationError.isEmpty {
                    Text(validationError)
                        .font(.caption)
                        .foregroundStyle(.red)
                }

                HStack {
                    Button("Cancel") { dismiss() }
                    Spacer()
                    Button("Save Changes") { save() }
                        .buttonStyle(.borderedProminent)
                        .tint(AddemTheme.purpleTint)
                }
            }
            .padding(18)
            .glassCard()
            .padding(14)
        }
    }

    private func save() {
        validationError = ""

        guard let url = URL(string: urlString), url.scheme != nil else {
            validationError = "Please enter a valid URL (including https://)."
            return
        }

        let tags = tagsCSV
            .split(separator: ",")
            .map { $0.trimmingCharacters(in: .whitespacesAndNewlines).lowercased() }
            .filter { !$0.isEmpty }

        let updated = LinkItem(
            id: original.id,
            urlString: url.absoluteString,
            title: title.isEmpty ? (url.host ?? "Untitled") : title,
            tags: Array(Set(tags)).sorted(),
            notes: notes,
            isPinned: isPinned,
            createdAt: original.createdAt,
            lastOpenedAt: original.lastOpenedAt
        )

        store.update(updated)
        dismiss()
    }
}
