Imports Microsoft.AspNetCore.Builder

Module Program
    Sub Main(args As String())
        Dim builder = WebApplication.CreateBuilder(args)
        builder.Services.AddWebFormsCore()

        Dim app = builder.Build()

        app.UsePage()

        app.Run()
    End Sub
End Module
