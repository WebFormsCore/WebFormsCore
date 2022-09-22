using System;

namespace WebFormsCore.Serializer;

public record ViewStateSerializerRegistration(byte Id, Type Type, Type SerializerType);