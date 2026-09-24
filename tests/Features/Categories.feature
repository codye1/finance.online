Feature: Categories page

Background:
    Given I am logged in and on the categories page

Scenario: Categories page is displayed
    Then I see the categories page header
    And I see the categories list

Scenario: Open and close the add category modal
    When I click the add category button
    Then the add category modal is visible
    When I close the category modal
    Then the add category modal is hidden

Scenario: Add category form requires a name
    Given I open the add category modal
    When I submit the category form without a name
    Then I see a validation error for the category name field

Scenario: Add category form rejects a name longer than 100 characters
    Given I open the add category modal
    When I fill in a category name of 101 characters
    And I submit the category form
    Then I see a validation error for the category name field
    And the add category modal is visible

Scenario: Submit button is disabled while the form is invalid
    Given I open the add category modal
    When I submit the category form without a name
    Then the category submit button is disabled

Scenario: Select a category color
    Given I open the add category modal
    When I select the category color "#10B981"
    Then the category color "#10B981" is active
    And the category color "#2563EB" is not active

Scenario: Successfully add a new category
    Given I open the add category modal
    When I fill in a unique category name
    And I submit the category form
    Then the add category modal is hidden
    And the new category appears in the categories list

Scenario: New category is created with the selected color
    Given I open the add category modal
    When I fill in a unique category name
    And I select the category color "#EF4444"
    And I submit the category form
    Then the new category appears in the categories list
    And the new category has the color "#EF4444"

Scenario: Delete a category and confirm
    Given I have created a new category
    When I delete that category and confirm
    Then that category is removed from the categories list

Scenario: Cancel deleting a category
    Given I have created a new category
    When I delete that category and cancel
    Then that category is still in the categories list
