import Foundation

enum SmartOrganizer {
    struct Draft {
        var urlString: String
        var title: String
        var notes: String
        var tags: [String]
    }

    static func draft(from pastedText: String) -> Draft {
        let trimmed = pastedText.trimmingCharacters(in: .whitespacesAndNewlines)
        let detectedURL = firstURL(in: trimmed)

        let urlString = detectedURL?.absoluteString ?? trimmed
        var notes = trimmed
        if let url = detectedURL?.absoluteString {
            notes = notes.replacingOccurrences(of: url, with: "")
                .trimmingCharacters(in: .whitespacesAndNewlines)
        }

        let title = defaultTitle(for: urlString)
        let tags = autoTags(urlString: urlString, title: title, notes: notes)
        return Draft(urlString: urlString, title: title, notes: notes, tags: tags)
    }

    static func defaultTitle(for urlString: String) -> String {
        if let host = URL(string: urlString)?.host?.replacingOccurrences(of: "www.", with: ""), !host.isEmpty {
            return host
        }
        return "New Link"
    }

    static func shouldReplaceTitle(currentTitle: String, urlString: String) -> Bool {
        let normalizedCurrent = currentTitle.trimmingCharacters(in: .whitespacesAndNewlines)
        if normalizedCurrent.isEmpty { return true }
        if normalizedCurrent == "New Link" { return true }

        let defaultTitle = defaultTitle(for: urlString).lowercased()
        return normalizedCurrent.lowercased() == defaultTitle
    }

    static func autoTags(urlString: String, title: String, notes: String) -> [String] {
        var bag = Set<String>()

        if let host = URL(string: urlString)?.host?.lowercased() {
            let clean = host.replacingOccurrences(of: "www.", with: "")
            let parts = clean.components(separatedBy: ".")
            if let first = parts.first, !first.isEmpty { bag.insert(first) }
            bag.insert(clean)
        }

        let tokens = (title + " " + notes)
            .lowercased()
            .components(separatedBy: CharacterSet.alphanumerics.inverted)
            .filter { $0.count >= 4 }

        for token in tokens.prefix(4) { bag.insert(token) }

        if bag.isEmpty { bag.insert("general") }
        return bag.sorted()
    }

    private static func firstURL(in text: String) -> URL? {
        let detector = try? NSDataDetector(types: NSTextCheckingResult.CheckingType.link.rawValue)
        let range = NSRange(location: 0, length: (text as NSString).length)
        let match = detector?.firstMatch(in: text, options: [], range: range)
        return match?.url
    }
}
