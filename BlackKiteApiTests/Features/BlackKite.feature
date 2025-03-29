Feature: Black Kite API Automation

  Scenario: Create ecosystem and scan a company
    Given Authenticate to Black Kite API
    When Create a new ecosystem
    Then Verified that ecosystem is created
    When Create a new company with domain "github.com"
    Then Verified that scan status "Extended Rescan Results Ready"
    When Get notifications for company "GitHub"
    Then All notifications should match the company id and name
    When Get findings for a random notification
    Then Findings should not be empty
    When Get finding detail for selected finding
    Then Verify that finding id is match with response
    When Update selected finding status
    Then Verify that finding status update action is logged
    When Delete the created company
    Then Verify that the company is deleted from created ecosystem
    When Delete the created ecosystem
    Then The ecosystem should no longer exist
