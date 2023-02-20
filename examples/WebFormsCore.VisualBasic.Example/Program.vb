Imports Microsoft.AspNetCore.Builder

Module Program
    Sub Main(args As String())
        Dim builder = WebApplication.CreateBuilder(args)
        builder.Services.AddWebForms()

        Dim app = builder.Build()

        app.MapAspx("/", "Default.aspx")
        app.MapFallbackToAspx()
        app.Run()
    End Sub
End Module
