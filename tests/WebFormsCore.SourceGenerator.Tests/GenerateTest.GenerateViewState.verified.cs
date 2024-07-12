//HintName: Tests_Example.cs
#nullable enable
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable IL2074 // Value stored in field 'target field' does not satisfy 'DynamicallyAccessedMembersAttribute' requirements.

using System;
using System.Collections;
using System.Collections.Generic;
using WebFormsCore;


namespace Tests
{
    public partial class Example : IViewStateObject
    {
        private DefaultViewStateValues _defaultViewStateValues = new DefaultViewStateValues();

        private void TrackViewState(ViewStateProvider provider)
        {
            provider.TrackViewState<string>(test);
            provider.TrackViewState<string>(test2);

            #nullable disable
            ref var defaultValues = ref _defaultViewStateValues;
            defaultValues.test = test;
            defaultValues.test2 = test2;
            #nullable restore
        }

		private void OnWriteViewState(ref ViewStateWriter writer)
        {

            if (!writer.Compact)
            {
                WriteViewStateWithFlag(ref writer);
            }
            else
            {
                WriteViewStateWithKeys(ref writer);
            }
        }

        private void WriteViewStateWithFlag(ref ViewStateWriter writer)
        {
            ref var defaultValues = ref _defaultViewStateValues;
            byte flag = 0;

            // test
            var writetest = (writer.StoreInViewState<string>(test, defaultValues.test));
            if (writetest) flag |= 1;

            // test2
            var writetest2 = (writer.StoreInViewState<string>(test2, defaultValues.test2)) && Validate;
            if (writetest2) flag |= 2;

            writer.Write(flag);
            if (writetest) writer.Write<string>(test, defaultValues.test);
            if (writetest2) writer.Write<string>(test2, defaultValues.test2);
        }

        private void WriteViewStateWithKeys(ref ViewStateWriter writer)
        {
            ref var defaultValues = ref _defaultViewStateValues;

            var length = 0;

            var writetest = (writer.StoreInViewState<string>(test, defaultValues.test));
            if (writetest) length++;

            var writetest2 = (writer.StoreInViewState<string>(test2, defaultValues.test2)) && Validate;
            if (writetest2) length++;

            writer.Write(length);

            if (writetest)
            {
                writer.Write("test");
                writer.Write<string>(test, defaultValues.test);
            }

            if (writetest2)
            {
                writer.Write("test2");
                writer.Write<string>(test2, defaultValues.test2);
            }
        }

		private void OnLoadViewState(ref ViewStateReader reader)
        {

            if (reader.Compact)
            {
                var flag = reader.Read<byte>();
                if ((flag & 1) != 0) test = reader.Read<string>(test)!;
                if ((flag & 2) != 0) test2 = reader.Read<string>(test2)!;
            }
            else
            {
                for (var length = reader.Read<int>(); length > 0; length--)
                {
                    var name = reader.Read<string>()!;
                    if (name.Equals("test", StringComparison.Ordinal)) test = reader.Read<string>(test)!;
                    if (name.Equals("test2", StringComparison.Ordinal)) test2 = reader.Read<string>(test2)!;
                }
            }
        }

        bool IViewStateObject.WriteToViewState => true;
        void IViewStateObject.TrackViewState(ViewStateProvider provider) => TrackViewState(provider);
        void IViewStateObject.WriteViewState(ref ViewStateWriter writer) => OnWriteViewState(ref writer);
        void IViewStateObject.ReadViewState(ref ViewStateReader reader) => OnLoadViewState(ref reader);

        private struct DefaultViewStateValues
        {
            public DefaultViewStateValues()
            {
                test = default!;
                test2 = default!;
            }
            public string test { get; set; }
            public string test2 { get; set; }
        }
    }
}
