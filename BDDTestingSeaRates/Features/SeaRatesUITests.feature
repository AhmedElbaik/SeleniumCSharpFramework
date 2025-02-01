Feature: Find Freight Shipping Rates

  As a user,
  I want to find the best freight shipping rates
  So that I can plan my shipments effectively.

  @freightRates
  Scenario: Find shipping rates from one city to another on a specific date
    Given I am on the SeaRates landing page
    And I accept the cookies policy
    #And I successfully Login to the app
    When I enter "Cairo, EG" as the origin city
    And I enter "Istanbul, TR" as the destination city
    #And I select random shipping date
    #And I select "FCL, 20'ST" as the container type
    #And I click on the "Find Rates" button
    #Then I should see a list of available freight rates
    #And the rates should be displayed for the selected date and container type