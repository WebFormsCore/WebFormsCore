namespace WebFormsCore.UI.WebControls;

/// <summary>Specifies the unit of measurement.</summary>
public enum UnitType
{
    /// <summary>Measurement is in pixels.</summary>
    Pixel = 1,
    /// <summary>Measurement is in points. A point represents 1/72 of an inch.</summary>
    Point = 2,
    /// <summary>Measurement is in picas. A pica represents 12 points.</summary>
    Pica = 3,
    /// <summary>Measurement is in inches.</summary>
    Inch = 4,
    /// <summary>Measurement is in millimeters.</summary>
    Mm = 5,
    /// <summary>Measurement is in centimeters.</summary>
    Cm = 6,
    /// <summary>Measurement is a percentage relative to the parent element.</summary>
    Percentage = 7,
    /// <summary>Measurement is relative to the height of the parent element's font.</summary>
    Em = 8,
    /// <summary>Measurement is relative to the height of the lowercase letter x of the parent element's font.</summary>
    Ex = 9,
}
