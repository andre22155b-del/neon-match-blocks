import Foundation

struct LinkItem: Identifiable, Codable, Hashable {
    var id: UUID = UUID()
    var urlString: String
    var title: String
    var tags: [String]
    var notes: String
    var isPinned: Bool
    var createdAt: Date = Date()
    var lastOpenedAt: Date?
    var aiSummary: String? = nil
    var aiConfidence: Double? = nil
    var aiUpdatedAt: Date? = nil

    var url: URL? { URL(string: urlString) }

    var domain: String {
        guard let host = url?.host, !host.isEmpty else { return "unknown" }
        return host.replacingOccurrences(of: "www.", with: "")
    }
}
