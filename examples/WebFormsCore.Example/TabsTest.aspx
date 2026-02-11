<%@ Page language="C#" Inherits="WebFormsCore.Example.TabsTest" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server" id="Head">
    <meta charset="UTF-8"/>
    <title>Tabs Demo</title>
    <link rel="stylesheet" href="/bootstrap.min.css" />
    <style>
        [data-wfc-tabs] [role="tablist"] {
            list-style: none;
            display: flex;
            padding: 0;
            margin: 0;
            border-bottom: 2px solid #dee2e6;
        }
        [data-wfc-tabs] [role="tablist"] li {
            margin: 0;
        }
        [data-wfc-tabs] [role="tab"] {
            background: none;
            border: none;
            padding: 0.5rem 1rem;
            cursor: pointer;
            border-bottom: 2px solid transparent;
            margin-bottom: -2px;
            color: #6c757d;
        }
        [data-wfc-tabs] [role="tab"][aria-selected="true"] {
            color: #0d6efd;
            border-bottom-color: #0d6efd;
        }
        [data-wfc-tabs] [role="tab"][disabled] {
            color: #adb5bd;
            cursor: not-allowed;
        }
        [data-wfc-tabs] [role="tabpanel"] {
            padding: 1rem 0;
        }
    </style>
</head>
<body runat="server" id="Body">
<div class="container py-4">
    <h1>TabControl Demo</h1>

    <div class="row mt-4">
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <strong>Basic Tabs</strong>
                </div>
                <div class="card-body">
                    <wfc:TabControl runat="server" ID="basicTabs">
                        <Tabs>
                            <wfc:Tab runat="server" Title="Overview">
                                <TabContent>
                                    <p>This is the overview panel. Switching between loaded tabs happens entirely client-side.</p>
                                </TabContent>
                            </wfc:Tab>
                            <wfc:Tab runat="server" Title="Details">
                                <TabContent>
                                    <p>Detail information goes here. No postback occurred when you clicked this tab.</p>
                                </TabContent>
                            </wfc:Tab>
                            <wfc:Tab runat="server" Title="Disabled" Enabled="false">
                                <TabContent>
                                    <p>You should not see this content.</p>
                                </TabContent>
                            </wfc:Tab>
                        </Tabs>
                    </wfc:TabControl>
                </div>
            </div>
        </div>

        <div class="col-md-6">
            <div class="card">
                <div class="card-header d-flex justify-content-between align-items-center">
                    <strong>Lazy Loading Tabs</strong>
                    <wfc:Label runat="server" ID="lblStatus" CssClass="badge bg-secondary" />
                </div>
                <div class="card-body">
                    <wfc:TabControl runat="server" ID="lazyTabs">
                        <Tabs>
                            <wfc:Tab runat="server" Title="Loaded">
                                <TabContent>
                                    <p>This tab content was loaded with the page.</p>
                                </TabContent>
                            </wfc:Tab>
                            <wfc:Tab runat="server" ID="lazyTab" Title="Lazy Tab" LazyLoadContent="true" OnContentLoaded="OnLazyTabContentLoaded">
                                <TabContent>
                                    <wfc:Label runat="server" ID="lblResult">Loading...</wfc:Label>
                                </TabContent>
                            </wfc:Tab>
                        </Tabs>
                    </wfc:TabControl>
                </div>
            </div>
        </div>
    </div>

    <div class="row mt-4">
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <strong>Custom Header Content</strong>
                </div>
                <div class="card-body">
                    <wfc:TabControl runat="server" ID="headerTabs">
                        <Tabs>
                            <wfc:Tab runat="server" Title="Home">
                                <HeaderContent>
                                    <strong>Home</strong>
                                </HeaderContent>
                                <TabContent>
                                    <p>Tabs can have custom header templates with arbitrary HTML.</p>
                                </TabContent>
                            </wfc:Tab>
                            <wfc:Tab runat="server" Title="Settings">
                                <HeaderContent>
                                    <em>Settings</em>
                                </HeaderContent>
                                <TabContent>
                                    <p>This tab header uses <code>&lt;em&gt;</code> styling.</p>
                                </TabContent>
                            </wfc:Tab>
                        </Tabs>
                    </wfc:TabControl>
                </div>
            </div>
        </div>

        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <strong>AutoPostBack Event</strong>
                </div>
                <div class="card-body">
                    <wfc:TabControl runat="server" ID="eventTabs" ActiveTabChanged="OnEventTabChanged">
                        <Tabs>
                            <wfc:Tab runat="server" Title="Tab A">
                                <TabContent>
                                    <p>Content A</p>
                                </TabContent>
                            </wfc:Tab>
                            <wfc:Tab runat="server" Title="Tab B" AutoPostBack="true">
                                <TabContent>
                                    <p>Content B (AutoPostBack triggers ActiveTabChanged on click)</p>
                                </TabContent>
                            </wfc:Tab>
                        </Tabs>
                    </wfc:TabControl>
                    <div class="mt-2">
                        <wfc:Label runat="server" ID="lblEventLog" CssClass="text-muted" Text="No tab change event yet." />
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>
</body>
</html>
