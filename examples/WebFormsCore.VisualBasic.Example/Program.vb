Imports Microsoft.AspNetCore.Builder
Imports Microsoft.Extensions.DependencyInjection
Imports WebFormsCore

Module Program
    Sub Main(args As String())
        Dim builder = WebApplication.CreateBuilder(args)
        builder.Services.AddSystemWebAdapters()
        builder.Services.AddWebForms()

        Dim app = builder.Build()

        app.UseWebForms()
        app.Run()
    End Sub
End Module
