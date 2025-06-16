Feature: HttpClientServiceDiscovery
  Ensure HttpMetricsClient picks up the base address from service discovery

  Scenario: Resolve base address via environment variable
    Given service discovery variable is "http://example.com"
    When the metrics client is created
    Then the metrics client base address should be "http://example.com/"
