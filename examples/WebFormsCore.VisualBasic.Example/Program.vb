Imports HttpStack.AspNetCore
Imports Microsoft.AspNetCore.Builder

Module Program
    Sub Main(args As String())
        Dim builder = WebApplication.CreateBuilder(args)
        builder.Services.AddWebForms()

        Dim app = builder.Build()

        app.UseStack(sub(stack)
            stack.UsePage()
        End Sub)

        app.Run()
    End Sub
End Module
