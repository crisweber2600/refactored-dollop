namespace ExampleLib.Domain;

public record ValidationRule(ValidationStrategy Strategy, double Threshold);
