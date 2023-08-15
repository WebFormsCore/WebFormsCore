//HintName: WebForms.ViewState.cs
#nullable enable
#pragma warning disable CS8601 // Possible null reference assignment.

using System;
using System.Collections;
using System.Collections.Generic;
using WebFormsCore;


namespace Tests
{
    public partial class Example : IViewStateObject
    {
        private DefaultViewStateValues? _defaultViewStateValues;

        private void TrackViewState(ViewStateProvider provider)
        {
            provider.TrackViewState<string>(test);
            provider.TrackViewState<string>(test2);

            #nullable disable
            _defaultViewStateValues = new DefaultViewStateValues
            {
                test = test,
                test2 = test2,
            };
            #nullable restore
        }

		private void OnWriteViewState(ref ViewStateWriter writer)
        {

            var defaultValues = _defaultViewStateValues;

            byte flag = 0;

            // test
            var writetest = (defaultValues == null || writer.StoreInViewState<string>(test, defaultValues.test));
            if (writetest) flag |= 1;

            // test2
            var writetest2 = (defaultValues == null || writer.StoreInViewState<string>(test2, defaultValues.test2)) && Validate;
            if (writetest2) flag |= 2;

            writer.Write(flag);
            if (writetest) writer.Write<string>(test, defaultValues == null ? default : defaultValues.test);
            if (writetest2) writer.Write<string>(test2, defaultValues == null ? default : defaultValues.test2);
        }

		private void OnLoadViewState(ref ViewStateReader reader)
        {

            var flag = reader.Read<byte>();
            if ((flag & 1) != 0) test = reader.Read<string>(test);
            if ((flag & 2) != 0) test2 = reader.Read<string>(test2);
        }

        bool IViewStateObject.WriteToViewState => true;
        void IViewStateObject.TrackViewState(ViewStateProvider provider) => TrackViewState(provider);
        void IViewStateObject.WriteViewState(ref ViewStateWriter writer) => OnWriteViewState(ref writer);
        void IViewStateObject.ReadViewState(ref ViewStateReader reader) => OnLoadViewState(ref reader);

        private class DefaultViewStateValues
        {
            public string test { get; set; } = default!;
            public string test2 { get; set; } = default!;
        }
    }
}
