Feature: DemoApiIntegration
  Verify that the generic HTTP client can retrieve metrics from the demo API

  Scenario: Fetch metrics from the demo API
    Given the demo API is running
    When the http client requests "http://localhost:5000/metrics" using GET
    Then the response list should contain 3 items
