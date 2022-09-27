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

            byte flag = 0;
            var writetest = test != default(string);
            if (writetest) flag |= 1;
            var writetest2 = test2 != default(string) && Validate;
            if (writetest2) flag |= 2;

            writer.Write(flag);
            if (writetest) writer.Write<string>(test);
            if (writetest2) writer.Write<string>(test2);
        }

		protected override void OnLoadViewState(ref ViewStateReader reader)
        {
            base.OnLoadViewState(ref reader);

            var flag = reader.Read<byte>();
            if ((flag & 1) != 0) test = reader.Read<string>();
            if ((flag & 2) != 0) test2 = reader.Read<string>();
        }

        void IViewStateObject.WriteViewState(ref ViewStateWriter writer) => OnWriteViewState(ref writer);
        void IViewStateObject.ReadViewState(ref ViewStateReader reader) => OnLoadViewState(ref reader);
    }
}
