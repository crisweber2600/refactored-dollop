Feature: GatherApiData
  Retrieves metric data from external APIs.

  Scenario: Successfully retrieve metrics from a single endpoint
    Given the API endpoint "https://example.com/metrics" is available
    When the system requests metric data
    Then the API should respond with HTTP 200
    And the response should contain metric values
      | MetricValue |
      | 42.0 |
      | 43.1 |
      | 41.7 |

  Scenario: Fail to retrieve metrics from an unavailable endpoint
    Given the API endpoint "https://fail.com/metrics" is down
    When the system requests metric data
    Then the API should respond with HTTP 503
    And the system should raise a "DataUnavailable" error

  Scenario: Handle API timeout gracefully
    Given the API endpoint responds after 10 seconds
    And the system has a timeout threshold of 5 seconds
    When the system requests metric data
    Then the request should be aborted
    And the system should record a "Timeout" error

  Scenario Outline: API returns malformed or unexpected data
    Given the API endpoint "<Endpoint>" returns content "<ContentType>"
    When the system attempts to parse the response
    Then the system should raise a "<Error>"

    Examples:
      | Endpoint | ContentType | Error |
      | https://example.com/json  | HTML  | InvalidFormat     |
      | https://example.com/empty | empty | NoContentReturned |
      | https://example.com/xml   | XML   | UnsupportedFormat |
