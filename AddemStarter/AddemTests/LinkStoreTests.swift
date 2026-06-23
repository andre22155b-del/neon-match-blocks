import XCTest
@testable import Addem

@MainActor
final class LinkStoreTests: XCTestCase {
    func testAddAndReload() throws {
        let temp = FileManager.default.temporaryDirectory
            .appendingPathComponent(UUID().uuidString)
            .appendingPathComponent("links.json")

        let store = LinkStore(fileURL: temp)
        let item = LinkItem(
            urlString: "https://example.com",
            title: "Example",
            tags: ["example"],
            notes: "test",
            isPinned: true
        )

        store.add(item)
        XCTAssertEqual(store.links.count, 1)

        let freshStore = LinkStore(fileURL: temp)
        XCTAssertEqual(freshStore.links.count, 1)
        XCTAssertEqual(freshStore.links.first?.title, "Example")
        XCTAssertEqual(freshStore.links.first?.isPinned, true)
    }

    func testAutoOrganizerExtractsURLAndTags() {
        let draft = SmartOrganizer.draft(from: "https://developer.apple.com SwiftUI docs for widgets")

        XCTAssertTrue(draft.urlString.contains("developer.apple.com"))
        XCTAssertFalse(draft.tags.isEmpty)
    }
}
