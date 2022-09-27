//HintName: WebForms.ViewState.cs
using System;
using System.Collections;
using System.Collections.Generic;
using WebFormsCore;


namespace Tests
{
    public partial class Example : IViewStateObject
    {
		protected override void OnWriteViewState(ref ViewStateWriter writer)
        {
            base.OnWriteViewState(ref writer);
            writer.Write<string>(test);
            if (Validate) writer.Write<string>(test2);
        }

		protected override void OnLoadViewState(ref ViewStateReader reader)
        {
            base.OnLoadViewState(ref reader);
            test = reader.Read<string>();
            if (Validate) test2 = reader.Read<string>();
        }

        void IViewStateObject.WriteViewState(ref ViewStateWriter writer) => OnWriteViewState(ref writer);
        void IViewStateObject.ReadViewState(ref ViewStateReader reader) => OnLoadViewState(ref reader);
    }
}
