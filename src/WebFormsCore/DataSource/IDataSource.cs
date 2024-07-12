using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace WebFormsCore;

public interface IDataSource
{
    object Value { get; }

    Type ElementType { get; }

    ValueTask LoadAsync(IDataSourceConsumer consumer);
}