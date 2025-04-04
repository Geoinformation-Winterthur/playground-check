using playground_check.Model;

public class AppStateProvider
{
    public Playground playground { get; set; } = new Playground();
    public string selectedInspectionType { get; set; } = "";
}
