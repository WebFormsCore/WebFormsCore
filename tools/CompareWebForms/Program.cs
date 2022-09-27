using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

const string webFormsNamespace = "System.Web";

var controlType = typeof(System.Web.UI.Control);
var allControls = controlType.Assembly.GetTypes()
    .Where(t => t.IsSubclassOf(controlType))
    .OrderBy(i => i.Name);

var webFormsCore = typeof(WebFormsCore.UI.Control).Assembly;
var controls = new List<ControlDefinition>();

foreach (var control in allControls)
{
    var namespaceName = control.Namespace;

    if (namespaceName != null && namespaceName.StartsWith(webFormsNamespace))
    {
        namespaceName = namespaceName.Substring(webFormsNamespace.Length).TrimStart('.');
    }

    var typeSuffix = namespaceName != null ? $"{namespaceName}.{control.Name}" : control.Name;
    var controlName = $"WebFormsCore.{typeSuffix}";
    var coreControl = webFormsCore.GetType(controlName);
    var hasAllSymbols = true;

    // Properties
    var coreProperties = coreControl?.GetProperties().ToDictionary(p => p.Name, p => p) ?? new Dictionary<string, PropertyInfo>();
    var properties = new List<ControlProperty>();

    foreach (var property in control.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).OrderBy(i => i.Name))
    {
        var added = coreProperties.TryGetValue(property.Name, out var coreProperty);
        var typeSame = added && (property.PropertyType.FullName == coreProperty.PropertyType.FullName || property.PropertyType.FullName?.Replace("System.Web", "WebFormsCore") == coreProperty.PropertyType.FullName);

        if (!added)
        {
            hasAllSymbols = false;
        }

        properties.Add(new ControlProperty(
            property,
            added,
            typeSame
        ));
    }

    // Events
    var coreEvents = coreControl?.GetEvents().Select(e => e.Name).ToList() ?? new List<string>();
    var events = new List<ControlEvent>();

    foreach (var @event in control.GetEvents(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).OrderBy(i => i.Name))
    {
        if (!coreEvents.Contains(@event.Name))
        {
            hasAllSymbols = false;
        }

        events.Add(new ControlEvent(
            @event,
            coreEvents.Contains(@event.Name)
        ));
    }

    controls.Add(new ControlDefinition(
        control,
        "user-content-" + control.Name.ToLowerInvariant(),
        coreControl != null,
        hasAllSymbols,
        properties,
        events
    ));
}

var sb = new StringBuilder();
sb.AppendLine("This issue lists controls in WebForms, with their status what is implemented in WebFormsCore.");
sb.AppendLine();
sb.AppendLine($"Controls: {controls.Count(c => c.Added)}/{controls.Count}");
sb.AppendLine($"Properties: {controls.Sum(c => c.Properties.Count(p => p.Added))}/{controls.Sum(c => c.Properties.Count)}");
sb.AppendLine($"Events: {controls.Sum(c => c.Events.Count(e => e.Added))}/{controls.Sum(c => c.Events.Count)}");
sb.AppendLine();
sb.AppendLine("| Control | Status | Properties | Events |");
sb.AppendLine("| ------- | ------ | ---------- | ------ |");

foreach (var control in controls)
{
    var statusIcon = control switch
    {
        { Added: true, FullyImplemented: true } => "✅",
        { Added: true, FullyImplemented: false } => "⚠",
        _ => "❌",
    };

    sb.Append("| ");
    sb.Append(control.HasSymbols ? @$"<a href=""#{control.Id}"">{control.Type.Name}</a>" : control.Type.Name);
    sb.Append($" | {statusIcon}");
    sb.Append($" | {control.Properties.Count(i => i.Added)}/{control.Properties.Count}");
    sb.Append($" | {control.Events.Count(i => i.Added)}/{control.Events.Count}");
    sb.AppendLine(" |");
}

foreach (var control in controls.Where(c => c.HasSymbols))
{
    sb.AppendLine();
    sb.AppendLine(@$"<h2 id=""{control.Id}"">{control.Type.Name}</h2>");
    sb.AppendLine();
    sb.AppendLine($"**Namespace:** {control.Type.Namespace}");

    if (control.Properties.Count > 0)
    {
        sb.AppendLine();
        sb.AppendLine("### Properties");
        sb.AppendLine("| Property | Type | Status |");
        sb.AppendLine("| -------- | ---- | ------ |");
        foreach (var property in control.Properties)
        {
            var statusIcon = property switch
            {
                { Added: true, TypeSame: true } => "✅",
                { Added: true, TypeSame: false } => "⚠",
                _ => "❌",
            };

            sb.Append($"| {property.Property.Name}");
            sb.Append($" | {property.Property.PropertyType}");

            if (property.Added & !property.TypeSame)
            {
                sb.Append(" **`INCORRECT`**");
            }

            sb.Append($" | {statusIcon}");
            sb.AppendLine(" |");
        }
    }

    if (control.Events.Count > 0)
    {
        sb.AppendLine();
        sb.AppendLine("### Events");
        sb.AppendLine("| Event | Status |");
        sb.AppendLine("| ----- | ------ |");
        foreach (var @event in control.Events)
        {
            var statusIcon = @event.Added ? "✅" : "❌";

            sb.AppendLine($"| {@event.Event.Name} | {statusIcon} |");
        }
    }
}

File.WriteAllText("results.md", sb.ToString());

record ControlProperty(PropertyInfo Property, bool Added, bool TypeSame);

record ControlEvent(EventInfo Event, bool Added);

record ControlDefinition(Type Type, string Id, bool Added, bool FullyImplemented, List<ControlProperty> Properties, List<ControlEvent> Events)
{
    public bool HasSymbols => Properties.Count > 0;
}
