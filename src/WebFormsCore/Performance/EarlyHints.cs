using System;
using System.Collections.Generic;
using System.Text;

namespace WebFormsCore.Performance;

public class EarlyHints
{
    private readonly List<EarlyHintRegistration> _registrations = new();

    public bool IsEmpty => _registrations.Count == 0;

    public bool Enabled { get; set; }

    public void Add(string location, EarlyHintRelation relation, EarlyHintType type, bool crossOrigin = false)
    {
        _registrations.Add(new EarlyHintRegistration
        {
            Location = location,
            Relation = relation,
            Type = type,
            CrossOrigin = crossOrigin
        });
    }

    public void AddStyle(string location, bool crossOrigin = false) => Add(location, EarlyHintRelation.Preload, EarlyHintType.Style, crossOrigin);

    public void AddScript(string location, bool crossOrigin = false) => Add(location, EarlyHintRelation.Preload, EarlyHintType.Script, crossOrigin);

    public string GetLinkHeader()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < _registrations.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            var registration = _registrations[i];

            var relation = registration.Relation switch
            {
                EarlyHintRelation.Preload => "preload",
                EarlyHintRelation.Prefetch => "prefetch",
                _ => throw new ArgumentOutOfRangeException()
            };

            var type = registration.Type switch
            {
                EarlyHintType.Style => "style",
                EarlyHintType.Script => "script",
                EarlyHintType.Image => "image",
                EarlyHintType.Font => "font",
                _ => throw new ArgumentOutOfRangeException()
            };

            builder.Append($"<{registration.Location}>; rel={relation}; as={type}");

            if (registration.CrossOrigin)
            {
                builder.Append("; crossorigin");
            }
        }

        return builder.ToString();
    }
}