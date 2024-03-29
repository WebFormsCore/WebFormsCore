﻿namespace WebFormsCore.UI;

public class JsInclude : ClientDependencyInclude
{
    public JsInclude()
    {
        DependencyType = ClientDependencyType.Javascript;
    }

    public JsInclude(IClientDependencyFile file)
        : base(file)
    {
        DependencyType = ClientDependencyType.Javascript;
    }
}