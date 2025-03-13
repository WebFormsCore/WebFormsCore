namespace WebFormsCore.Performance;

public struct EarlyHintRegistration
{
    public string Location { get; set; }

    public EarlyHintRelation Relation { get; set; }

    public EarlyHintType Type { get; set; }

    public bool CrossOrigin { get; set; }
}