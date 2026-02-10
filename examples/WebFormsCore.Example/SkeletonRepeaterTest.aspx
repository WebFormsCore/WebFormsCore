<%@ Page language="C#" Inherits="WebFormsCore.Example.SkeletonRepeaterTest" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server" id="Head">
    <meta charset="UTF-8"/>
    <title>Skeleton Repeater &amp; Grid Demo</title>
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
        @keyframes skeleton-shimmer {
            0% { background-position: 200% 0; }
            100% { background-position: -200% 0; }
        }
    </style>
</head>
<body runat="server" id="Body">
<div class="container py-4">
    <h1>Skeleton Repeater &amp; Grid Demo</h1>
    <p class="text-muted">Content loads after a 2‑second delay. Skeleton placeholders are shown while loading.</p>

    <div class="row mt-4">
        <!-- Repeater skeleton demo -->
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <strong>Repeater (LazyLoader)</strong>
                </div>
                <div class="card-body">
                    <wfc:LazyLoader runat="server" ID="lazyRepeater">
                        <ul class="list-group">
                            <wfc:Repeater runat="server" ID="repeater" SkeletonItemCount="4" ItemType="WebFormsCore.Example.UserModel" OnItemDataBound="repeater_OnItemDataBound">
                                <ItemTemplate>
                                    <li class="list-group-item d-flex justify-content-between align-items-center">
                                        <wfc:Label runat="server" ID="lblName" />
                                        <span class="badge bg-primary rounded-pill">
                                            <wfc:Label runat="server" ID="lblRole" />
                                        </span>
                                    </li>
                                </ItemTemplate>
                            </wfc:Repeater>
                        </ul>
                    </wfc:LazyLoader>
                </div>
            </div>
        </div>

        <!-- Grid skeleton demo -->
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <strong>Grid (LazyLoader)</strong>
                </div>
                <div class="card-body">
                    <wfc:LazyLoader runat="server" ID="lazyGrid">
                        <wfc:Grid runat="server" ID="grid" CssClass="table table-striped" SkeletonItemCount="4">
                            <Columns>
                                <wfc:GridBoundColumn HeaderText="Name" DataField="Name" />
                                <wfc:GridBoundColumn HeaderText="Email" DataField="Email" />
                                <wfc:GridBoundColumn HeaderText="Role" DataField="Role" />
                            </Columns>
                        </wfc:Grid>
                    </wfc:LazyLoader>
                </div>
            </div>
        </div>
    </div>

    <!-- Static skeleton container demo -->
    <div class="row mt-4">
        <div class="col-12">
            <div class="card">
                <div class="card-header d-flex justify-content-between align-items-center">
                    <strong>SkeletonContainer (Repeater)</strong>
                    <wfc:Button runat="server" ID="btnToggle" Text="Toggle Loading" OnClick="btnToggle_OnClick" CssClass="btn btn-sm btn-outline-primary" />
                </div>
                <div class="card-body">
                    <wfc:SkeletonContainer runat="server" ID="skeletonContainer" Loading="True">
                        <wfc:Grid runat="server" ID="staticGrid" CssClass="table" SkeletonItemCount="3">
                            <Columns>
                                <wfc:GridBoundColumn HeaderText="Product" DataField="Product" />
                                <wfc:GridBoundColumn HeaderText="Price" DataField="Price" />
                            </Columns>
                        </wfc:Grid>
                    </wfc:SkeletonContainer>
                </div>
            </div>
        </div>
    </div>
</div>
</body>
</html>
