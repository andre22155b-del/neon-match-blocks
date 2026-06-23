import SwiftUI

struct AddLinkSheet: View {
    @Environment(\.dismiss) private var dismiss
    @EnvironmentObject private var store: LinkStore

    @State private var pastedText = ""
    @State private var urlString = ""
    @State private var title = ""
    @State private var tagsCSV = ""
    @State private var notes = ""
    @State private var isPinned = false
    @State private var validationError = ""

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Quick Add")
                .font(.title2.bold())

            Text("Paste any text with a URL and Addem will organize tags/notes automatically.")
                .font(.caption)
                .foregroundStyle(.secondary)

            TextEditor(text: $pastedText)
                .font(.body)
                .frame(height: 90)
                .padding(8)
                .background(Color.white.opacity(0.65))
                .clipShape(RoundedRectangle(cornerRadius: 12, style: .continuous))
                .onChange(of: pastedText) { _, newText in
                    autoFill(from: newText)
                }

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
                Button("Save Link") { save() }
                    .buttonStyle(.borderedProminent)
                    .tint(AddemTheme.purpleTint)
            }
        }
        .padding(18)
        .background(AddemTheme.panelGradient.opacity(0.5))
    }

    private func autoFill(from text: String) {
        guard !text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty else { return }
        let draft = SmartOrganizer.draft(from: text)
        urlString = draft.urlString
        if title.isEmpty || title == "New Link" { title = draft.title }
        if notes.isEmpty { notes = draft.notes }
        if tagsCSV.isEmpty { tagsCSV = draft.tags.joined(separator: ", ") }
    }

    private func save() {
        validationError = ""

        guard let url = URL(string: urlString), url.scheme != nil else {
            validationError = "Please enter a valid URL (including https://)."
            return
        }

        let manualTags = tagsCSV
            .split(separator: ",")
            .map { $0.trimmingCharacters(in: .whitespacesAndNewlines).lowercased() }
            .filter { !$0.isEmpty }

        let autoTags = SmartOrganizer.autoTags(urlString: url.absoluteString, title: title, notes: notes)
        let finalTags = Array(Set(manualTags + autoTags)).sorted()

        let item = LinkItem(
            urlString: url.absoluteString,
            title: title.isEmpty ? (url.host ?? "Untitled") : title,
            tags: finalTags,
            notes: notes,
            isPinned: isPinned
        )

        store.add(item)
        dismiss()
    }
}
