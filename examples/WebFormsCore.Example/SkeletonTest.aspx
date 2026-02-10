<%@ Page language="C#" Inherits="WebFormsCore.Example.SkeletonTest" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server" id="Head">
    <meta charset="UTF-8"/>
    <title>Skeleton Loading Test</title>
    <link rel="stylesheet" href="/bootstrap.min.css" />
    <style>
        .wfc-skeleton {
            background: linear-gradient(90deg, #e0e0e0 25%, #f0f0f0 50%, #e0e0e0 75%);
            background-size: 200% 100%;
            animation: skeleton-shimmer 1.5s infinite;
            border-radius: 4px;
        }
        .wfc-skeleton-text:after {
            content: "";
            min-width: 80px;
            display: inline-block;
        }
        .wfc-skeleton-button {
            min-width: 80px;
        }
        @keyframes skeleton-shimmer {
            0% { background-position: 200% 0; }
            100% { background-position: -200% 0; }
        }
    </style>
</head>
<body runat="server" id="Body">
<div class="container py-4">
    <h1>Skeleton Loading Demo</h1>

    <div class="row mt-4">
        <!-- SkeletonContainer demo -->
        <div class="col-md-6">
            <div class="card">
                <div class="card-header d-flex justify-content-between align-items-center">
                    <strong>SkeletonContainer</strong>
                    <wfc:Button runat="server" ID="btnToggle" Text="Toggle Loading" OnClick="btnToggle_OnClick" CssClass="btn btn-sm btn-outline-primary" />
                </div>
                <div class="card-body">
                    <wfc:SkeletonContainer runat="server" ID="skeleton" Loading="True">
                        <div class="mb-3">
                            <wfc:Label runat="server" ID="lblName" Text="Name:" CssClass="form-label" />
                            <wfc:TextBox runat="server" ID="txtName" CssClass="form-control" />
                        </div>
                        <div class="mb-3">
                            <wfc:Label runat="server" ID="lblEmail" Text="Email:" CssClass="form-label" />
                            <wfc:TextBox runat="server" ID="txtEmail" CssClass="form-control" />
                        </div>
                        <wfc:Button runat="server" ID="btnSubmit" Text="Submit" CssClass="btn btn-primary" />
                    </wfc:SkeletonContainer>
                </div>
            </div>
        </div>

        <!-- LazyLoader demo -->
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <strong>LazyLoader</strong>
                </div>
                <div class="card-body">
                    <wfc:LazyLoader runat="server" ID="lazyContent">
                        <div class="mb-3">
                            <wfc:Label runat="server" ID="lblLazyTitle" CssClass="form-label" />
                        </div>
                        <div class="mb-3">
                            <wfc:Label runat="server" ID="lblLazyTime" CssClass="form-label" />
                        </div>
                        <wfc:Button runat="server" ID="btnLazyAction" Text="Loaded!" CssClass="btn btn-success" />
                    </wfc:LazyLoader>
                </div>
            </div>
        </div>
    </div>
</div>
</body>
</html>
