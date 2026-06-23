import Foundation

struct AIEnrichmentResult {
    var title: String?
    var summary: String?
    var tags: [String]
    var confidence: Double
    var provider: String
}

protocol AIService {
    func enrich(item: LinkItem) async -> AIEnrichmentResult?
}

struct LocalAIService: AIService {
    func enrich(item: LinkItem) async -> AIEnrichmentResult? {
        let tags = SmartOrganizer.autoTags(
            urlString: item.urlString,
            title: item.title,
            notes: item.notes
        )

        var summary: String?
        if item.notes.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            summary = makeSummary(from: item)
        }

        let suggestedTitle = suggestedTitle(for: item)

        guard suggestedTitle != nil || summary != nil || !tags.isEmpty else {
            return nil
        }

        return AIEnrichmentResult(
            title: suggestedTitle,
            summary: summary,
            tags: tags,
            confidence: 0.55,
            provider: "local"
        )
    }

    private func suggestedTitle(for item: LinkItem) -> String? {
        let current = item.title.trimmingCharacters(in: .whitespacesAndNewlines)
        guard SmartOrganizer.shouldReplaceTitle(currentTitle: current, urlString: item.urlString) else {
            return nil
        }
        return SmartOrganizer.defaultTitle(for: item.urlString)
    }

    private func makeSummary(from item: LinkItem) -> String {
        let domain = item.domain
        let tagList = item.tags.prefix(2).joined(separator: ", ")
        if tagList.isEmpty {
            return "Saved from \(domain)."
        }
        return "Saved from \(domain). Topics: \(tagList)."
    }
}

struct OpenAIService: AIService {
    private let session: URLSession = .shared

    func enrich(item: LinkItem) async -> AIEnrichmentResult? {
        guard let apiKey = resolvedAPIKey(), !apiKey.isEmpty else {
            return nil
        }

        guard let request = makeRequest(item: item, apiKey: apiKey) else {
            return nil
        }

        do {
            let (data, response) = try await session.data(for: request)
            guard let http = response as? HTTPURLResponse, (200...299).contains(http.statusCode) else {
                return nil
            }
            return parseResponse(data: data)
        } catch {
            return nil
        }
    }

    private func resolvedAPIKey() -> String? {
        if let envKey = ProcessInfo.processInfo.environment["OPENAI_API_KEY"], !envKey.isEmpty {
            return envKey
        }
        let defaults = UserDefaults(suiteName: AppGroupConfig.groupID) ?? .standard
        return defaults.string(forKey: AppGroupConfig.openAIAPIKeyKey)
    }

    private func makeRequest(item: LinkItem, apiKey: String) -> URLRequest? {
        guard let url = URL(string: "https://api.openai.com/v1/chat/completions") else {
            return nil
        }

        let systemPrompt = """
        You enrich captured web links.
        Return strict JSON with keys:
        title (string), summary (string), tags (array of short lowercase strings), confidence (number 0..1).
        Keep summary <= 120 chars.
        """

        let userPrompt = """
        URL: \(item.urlString)
        Current title: \(item.title)
        Notes: \(item.notes)
        Existing tags: \(item.tags.joined(separator: ", "))
        """

        let payload: [String: Any] = [
            "model": "gpt-4.1-mini",
            "temperature": 0.2,
            "response_format": ["type": "json_object"],
            "messages": [
                ["role": "system", "content": systemPrompt],
                ["role": "user", "content": userPrompt]
            ]
        ]

        guard let body = try? JSONSerialization.data(withJSONObject: payload) else {
            return nil
        }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("Bearer \(apiKey)", forHTTPHeaderField: "Authorization")
        request.httpBody = body
        return request
    }

    private func parseResponse(data: Data) -> AIEnrichmentResult? {
        guard
            let root = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
            let choices = root["choices"] as? [[String: Any]],
            let firstChoice = choices.first,
            let message = firstChoice["message"] as? [String: Any],
            let content = message["content"] as? String,
            let raw = content.data(using: .utf8),
            let parsed = try? JSONSerialization.jsonObject(with: raw) as? [String: Any]
        else {
            return nil
        }

        let title = (parsed["title"] as? String)?.trimmedNilIfEmpty
        let summary = (parsed["summary"] as? String)?.trimmedNilIfEmpty
        let tags = ((parsed["tags"] as? [String]) ?? [])
            .map { $0.lowercased().trimmingCharacters(in: .whitespacesAndNewlines) }
            .filter { !$0.isEmpty }
        let confidence = min(max((parsed["confidence"] as? Double) ?? 0.72, 0), 1)

        guard title != nil || summary != nil || !tags.isEmpty else {
            return nil
        }

        return AIEnrichmentResult(
            title: title,
            summary: summary,
            tags: tags,
            confidence: confidence,
            provider: "openai"
        )
    }
}

struct CompositeAIService: AIService {
    private let remote: AIService
    private let local: AIService

    init(remote: AIService = OpenAIService(), local: AIService = LocalAIService()) {
        self.remote = remote
        self.local = local
    }

    func enrich(item: LinkItem) async -> AIEnrichmentResult? {
        let defaults = UserDefaults(suiteName: AppGroupConfig.groupID) ?? .standard
        let isEnabled = (defaults.object(forKey: AppGroupConfig.aiEnabledKey) as? Bool) ?? true
        guard isEnabled else { return nil }

        if let remoteResult = await remote.enrich(item: item) {
            return remoteResult
        }
        return await local.enrich(item: item)
    }
}

private extension String {
    var trimmedNilIfEmpty: String? {
        let trimmed = trimmingCharacters(in: .whitespacesAndNewlines)
        return trimmed.isEmpty ? nil : trimmed
    }
}
