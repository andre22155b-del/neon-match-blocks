import SwiftUI

struct MenuBarAddView: View {
    @EnvironmentObject private var store: LinkStore
    @Environment(\.colorScheme) private var colorScheme

    @State private var text = ""

    var body: some View {
        ZStack {
            AddemLiquidBackground()

            VStack(alignment: .leading, spacing: 8) {
                HStack(spacing: 8) {
                    brandWordmark
                    Text("Quick Capture")
                        .font(.caption.weight(.semibold))
                        .foregroundStyle(colorScheme == .dark ? Color.white.opacity(0.76) : AddemTheme.textSecondary)
                }

                TextField("Paste URL + optional notes", text: $text, axis: .vertical)
                    .lineLimit(3)
                    .textFieldStyle(.plain)
                    .padding(.horizontal, 10)
                    .padding(.vertical, 8)
                    .glassInputField(cornerRadius: 10)
                    .onSubmit {
                        save()
                    }

                Button("Save") { save() }
                .buttonStyle(.borderedProminent)
                .tint(AddemTheme.purpleTint)
                .keyboardShortcut(.defaultAction)
            }
            .frame(width: 300)
            .glassCard()
            .padding(8)
        }
    }

    private var brandWordmark: some View {
        let titleLead = colorScheme == .dark ? Color.white.opacity(0.95) : AddemTheme.purpleStrong
        let titleTrail = colorScheme == .dark ? AddemTheme.purpleTint : AddemTheme.purpleTint.opacity(0.95)
        let plateTop = colorScheme == .dark ? Color.white.opacity(0.24) : AddemTheme.purpleTint.opacity(0.30)
        let plateMid = colorScheme == .dark ? AddemTheme.purpleStrong.opacity(0.22) : AddemTheme.purpleStrong.opacity(0.16)
        let plateBottom = colorScheme == .dark ? Color.black.opacity(0.10) : Color.white.opacity(0.24)
        let border = colorScheme == .dark ? Color.white.opacity(0.56) : Color.white.opacity(0.72)

        return Text("AddEm")
            .font(.system(size: 18, weight: .black, design: .rounded))
            .foregroundStyle(
                LinearGradient(
                    colors: [titleLead, titleTrail],
                    startPoint: .topLeading,
                    endPoint: .bottomTrailing
                )
            )
            .padding(.horizontal, 10)
            .padding(.vertical, 5)
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
            .shadow(color: .black.opacity(0.10), radius: 4, x: 0, y: 2)
            .accessibilityLabel("AddEm")
    }

    private func save() {
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
}
