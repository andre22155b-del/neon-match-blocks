import Foundation

enum AddemAnalyticsEvent: String, CaseIterable {
    case linkSaved = "link_saved"
    case linkOpened = "link_opened"
    case linkPinned = "link_pinned"
    case linkRemoved = "link_removed"
    case searchUsed = "search_used"
}

struct AddemAnalyticsSnapshot {
    let dateKey: String
    let counts: [AddemAnalyticsEvent: Int]

    var totalEvents: Int {
        counts.values.reduce(0, +)
    }
}

final class AddemAnalytics {
    static let shared = AddemAnalytics()

    private let queue = DispatchQueue(label: "com.addem.analytics", qos: .utility)
    private let defaults: UserDefaults
    private let formatter: DateFormatter

    private init() {
        self.defaults = UserDefaults(suiteName: AppGroupConfig.groupID) ?? .standard
        self.formatter = DateFormatter()
        self.formatter.locale = Locale(identifier: "en_US_POSIX")
        self.formatter.timeZone = .current
        self.formatter.dateFormat = "yyyy-MM-dd"
    }

    func track(_ event: AddemAnalyticsEvent, metadata: [String: String] = [:], date: Date = Date()) {
        queue.sync {
            let day = dayKey(for: date)
            let dayCountKey = "\(storagePrefix).\(day).\(event.rawValue)"
            let totalCountKey = "\(storagePrefix).total.\(event.rawValue)"

            defaults.set(defaults.integer(forKey: dayCountKey) + 1, forKey: dayCountKey)
            defaults.set(defaults.integer(forKey: totalCountKey) + 1, forKey: totalCountKey)

            if metadata.isEmpty {
                print("[AddEmAnalytics] \(event.rawValue)")
            } else {
                let details = metadata
                    .sorted { $0.key < $1.key }
                    .map { "\($0.key)=\($0.value)" }
                    .joined(separator: ", ")
                print("[AddEmAnalytics] \(event.rawValue) | \(details)")
            }
        }
    }

    func snapshot(for date: Date = Date()) -> AddemAnalyticsSnapshot {
        queue.sync {
            let day = dayKey(for: date)
            var counts: [AddemAnalyticsEvent: Int] = [:]
            for event in AddemAnalyticsEvent.allCases {
                let key = "\(storagePrefix).\(day).\(event.rawValue)"
                counts[event] = defaults.integer(forKey: key)
            }
            return AddemAnalyticsSnapshot(dateKey: day, counts: counts)
        }
    }

    func printDailySummary(for date: Date = Date()) {
        let snapshot = snapshot(for: date)
        print("----- AddEm Day 1 Metrics (\(snapshot.dateKey)) -----")
        for event in AddemAnalyticsEvent.allCases {
            let count = snapshot.counts[event] ?? 0
            print("\(event.rawValue): \(count)")
        }
        print("total_events: \(snapshot.totalEvents)")
        print("-----------------------------------------------")
    }

    private var storagePrefix: String {
        "addem.analytics"
    }

    private func dayKey(for date: Date) -> String {
        formatter.string(from: date)
    }
}
