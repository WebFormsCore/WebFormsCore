using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

// ReSharper disable AccessToModifiedClosure

namespace WebFormsCore.Tests.Controls.Tabs;

public class TabControlTests(SeleniumFixture fixture)
{
    private static InlineTemplate TextTemplate(string text) =>
        new(c => c.Controls.AddWithoutPageEvents(new Label { Text = text }));

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderBasicTabs(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TabControl
        {
            ID = "tabs",
            Tabs =
            {
                new Tab
                {
                    Title = "Tab 1",
                    TabContent = TextTemplate("Content 1")
                },
                new Tab
                {
                    Title = "Tab 2",
                    TabContent = TextTemplate("Content 2")
                }
            }
        });

        // Should render two tab buttons
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        Assert.Equal(2, tabButtons.Count);
        Assert.Equal("Tab 1", tabButtons[0].Text);
        Assert.Equal("Tab 2", tabButtons[1].Text);

        // First tab should be active
        Assert.Equal("true", await tabButtons[0].GetAttributeAsync("aria-selected"));
        Assert.Equal("false", await tabButtons[1].GetAttributeAsync("aria-selected"));

        // Should render two tab panels
        var panels = await result.Browser.QuerySelectorAll("[role=tabpanel]").ToListAsync();
        Assert.Equal(2, panels.Count);

        // First panel should be visible, second should be hidden
        Assert.Contains("Content 1", panels[0].Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task SwitchTabOnClick(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TabControl
        {
            ID = "tabs",
            Tabs =
            {
                new Tab
                {
                    Title = "Tab 1",
                    TabContent = TextTemplate("Content 1")
                },
                new Tab
                {
                    Title = "Tab 2",
                    TabContent = TextTemplate("Content 2")
                }
            }
        });

        // Click the second tab (client-side switch, no postback)
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await tabButtons[1].ClickAsync();

        // Second tab should be active (switched client-side)
        tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        Assert.Equal("false", await tabButtons[0].GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await tabButtons[1].GetAttributeAsync("aria-selected"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task InvisibleTabNotRendered(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TabControl
        {
            ID = "tabs",
            Tabs =
            {
                new Tab
                {
                    Title = "Tab 1",
                    TabContent = TextTemplate("Content 1")
                },
                new Tab
                {
                    Title = "Hidden Tab",
                    Visible = false,
                    TabContent = TextTemplate("Hidden Content")
                },
                new Tab
                {
                    Title = "Tab 3",
                    TabContent = TextTemplate("Content 3")
                }
            }
        });

        // Should render only two tab buttons (hidden tab excluded)
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        Assert.Equal(2, tabButtons.Count);
        Assert.Equal("Tab 1", tabButtons[0].Text);
        Assert.Equal("Tab 3", tabButtons[1].Text);

        // Hidden content should not appear in the DOM
        var html = await result.Browser.GetHtmlAsync();
        Assert.DoesNotContain("Hidden Content", html);
        Assert.DoesNotContain("Hidden Tab", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task LazyLoadTabDoesNotRenderContentInitially(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TabControl
        {
            ID = "tabs",
            Tabs =
            {
                new Tab
                {
                    Title = "Tab 1",
                    TabContent = TextTemplate("Content 1")
                },
                new Tab
                {
                    Title = "Lazy Tab",
                    LazyLoadContent = true,
                    TabContent = TextTemplate("Lazy Content")
                }
            }
        });

        // First tab content should be visible
        var html = await result.Browser.GetHtmlAsync();
        Assert.Contains("Content 1", html);

        // Lazy tab content should NOT be rendered yet
        Assert.DoesNotContain("Lazy Content", html);

        // Lazy tab panel should contain a LazyLoader (data-wfc-lazy) that is not yet loaded
        var panels = await result.Browser.QuerySelectorAll("[role=tabpanel]").ToListAsync();
        Assert.Equal(2, panels.Count);
        var lazyLoader = result.Browser.QuerySelector("[role=tabpanel] [data-wfc-lazy]");
        Assert.NotNull(lazyLoader);
        Assert.Equal("true", await lazyLoader.GetAttributeAsync("aria-busy"));

        // But the lazy tab header should be visible
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        Assert.Equal(2, tabButtons.Count);
        Assert.Equal("Lazy Tab", tabButtons[1].Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task LazyLoadTabRendersContentWhenActivated(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TabControl
        {
            ID = "tabs",
            Tabs =
            {
                new Tab
                {
                    Title = "Tab 1",
                    TabContent = TextTemplate("Content 1")
                },
                new Tab
                {
                    Title = "Lazy Tab",
                    LazyLoadContent = true,
                    TabContent = TextTemplate("Lazy Content")
                }
            }
        });

        // Lazy content should not be present initially
        var html = await result.Browser.GetHtmlAsync();
        Assert.DoesNotContain("Lazy Content", html);

        // Click the lazy tab (triggers scoped postback via LazyLoader)
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await tabButtons[1].ClickAsync();

        // After postback, lazy content should now be rendered
        html = await result.Browser.GetHtmlAsync();
        Assert.Contains("Lazy Content", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task LazyLoadTabRetainsContentAfterSwitchingAway(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TabControl
        {
            ID = "tabs",
            Tabs =
            {
                new Tab
                {
                    Title = "Tab 1",
                    TabContent = TextTemplate("Content 1")
                },
                new Tab
                {
                    Title = "Lazy Tab",
                    LazyLoadContent = true,
                    TabContent = TextTemplate("Lazy Content")
                }
            }
        });

        // Activate the lazy tab (postback)
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await tabButtons[1].ClickAsync();

        // Verify lazy content loaded
        var html = await result.Browser.GetHtmlAsync();
        Assert.Contains("Lazy Content", html);

        // Switch back to the first tab (client-side, no postback)
        tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await tabButtons[0].ClickAsync();

        // The lazy tab content should still be in the DOM (just hidden)
        html = await result.Browser.GetHtmlAsync();
        Assert.Contains("Lazy Content", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ActiveTabChangedEventFires(Browser type)
    {
        var eventFired = false;

        await using var result = await fixture.StartAsync(type, () =>
        {
            var label = new Ref<Label>();
            var button = new Button { ID = "postbackBtn", Text = "Submit" };

            var tabControl = new TabControl
            {
                ID = "tabs",
                Tabs =
                {
                    new Tab
                    {
                        Title = "Tab 1",
                        TabContent = new InlineTemplate(c =>
                            c.Controls.AddWithoutPageEvents(new Label { Ref = label, Text = "Initial" }))
                    },
                    new Tab
                    {
                        Title = "Tab 2",
                        TabContent = new InlineTemplate(c =>
                            c.Controls.AddWithoutPageEvents(new Literal { Text = "Content 2" }))
                    }
                }
            };

            tabControl.ActiveTabChanged += (_, _) =>
            {
                eventFired = true;
                label.Value.Text = "Changed";
                return Task.CompletedTask;
            };

            return new Panel { Controls = [tabControl, button] };
        });

        // Switch to Tab 2 client-side
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await tabButtons[1].ClickAsync();

        // Trigger a full page postback so the submit hook syncs the active tab index
        var submitBtn = result.Browser.QuerySelector("#postbackBtn");
        Assert.NotNull(submitBtn);
        await submitBtn.ClickAsync();

        // The event should have fired and updated the label
        Assert.True(eventFired);
        Assert.Contains("Changed", await result.Browser.GetHtmlAsync());
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task InitialActiveTabIndex(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TabControl
        {
            ID = "tabs",
            ActiveTabIndex = 1,
            Tabs =
            {
                new Tab
                {
                    Title = "Tab 1",
                    TabContent = TextTemplate("Content 1")
                },
                new Tab
                {
                    Title = "Tab 2",
                    TabContent = TextTemplate("Content 2")
                }
            }
        });

        // Tab 2 should be initially active
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        Assert.Equal("false", await tabButtons[0].GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await tabButtons[1].GetAttributeAsync("aria-selected"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task LazyLoadFirstTabActiveRendersContent(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TabControl
        {
            ID = "tabs",
            Tabs =
            {
                new Tab
                {
                    Title = "Lazy First",
                    LazyLoadContent = true,
                    TabContent = TextTemplate("Lazy First Content")
                },
                new Tab
                {
                    Title = "Tab 2",
                    TabContent = TextTemplate("Content 2")
                }
            }
        });

        // First tab is active and lazy-loaded - since it's initially active,
        // its content should be rendered
        var html = await result.Browser.GetHtmlAsync();
        Assert.Contains("Lazy First Content", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task EmptyTabControlRendersWithoutError(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TabControl
        {
            ID = "tabs"
        });

        // Should render the container div with data-wfc-tabs
        var html = await result.Browser.GetHtmlAsync();
        Assert.Contains("data-wfc-tabs", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task TabPanelHasProperAriaAttributes(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TabControl
        {
            ID = "tabs",
            Tabs =
            {
                new Tab
                {
                    Title = "Tab 1",
                    TabContent = TextTemplate("Content 1")
                }
            }
        });

        // Tab panel should have role="tabpanel"
        var panel = result.Browser.QuerySelector("[role=tabpanel]");
        Assert.NotNull(panel);

        // Tab button should have aria-controls pointing to the panel
        var tabButton = result.Browser.QuerySelector("[role=tab]");
        Assert.NotNull(tabButton);
        var controlsId = await tabButton.GetAttributeAsync("aria-controls");
        Assert.NotNull(controlsId);
        Assert.NotEmpty(controlsId);

        // The panel should have an id matching aria-controls
        var panelId = await panel.GetAttributeAsync("id");
        Assert.Equal(controlsId, panelId);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task DisabledTabCannotBeActivated(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TabControl
        {
            ID = "tabs",
            Tabs =
            {
                new Tab
                {
                    Title = "Tab 1",
                    TabContent = TextTemplate("Content 1")
                },
                new Tab
                {
                    Title = "Disabled Tab",
                    Enabled = false,
                    TabContent = TextTemplate("Disabled Content")
                }
            }
        });

        // Disabled tab button should have disabled attribute
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        Assert.Equal(2, tabButtons.Count);
        Assert.NotNull(await tabButtons[1].GetAttributeAsync("disabled"));

        // Disabled tab button should NOT have data-wfc-postback
        Assert.Null(await tabButtons[1].GetAttributeAsync("data-wfc-postback"));

        // First tab should still be active after clicking disabled tab
        await tabButtons[1].ClickAsync();
        tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        Assert.Equal("true", await tabButtons[0].GetAttributeAsync("aria-selected"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task CustomHeaderContentRendered(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TabControl
        {
            ID = "tabs",
            Tabs =
            {
                new Tab
                {
                    Title = "Tab 1",
                    HeaderContent = new InlineTemplate(c =>
                        c.Controls.AddWithoutPageEvents(new Literal { Text = "<strong>Bold Tab</strong>", Mode = LiteralMode.PassThrough })),
                    TabContent = TextTemplate("Content 1")
                }
            }
        });

        // The tab button should contain the custom header HTML
        var html = await result.Browser.GetHtmlAsync();
        Assert.Contains("<strong>Bold Tab</strong>", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task HeaderContentFallsBackToTitle(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TabControl
        {
            ID = "tabs",
            Tabs =
            {
                new Tab
                {
                    Title = "Fallback Title",
                    TabContent = TextTemplate("Content 1")
                }
            }
        });

        // Without HeaderContent, the Title should be used
        var tabButton = result.Browser.QuerySelector("[role=tab]");
        Assert.NotNull(tabButton);
        Assert.Equal("Fallback Title", tabButton.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task LazyTabContentPersistsAfterSecondLazyPostback(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new TabControl
        {
            ID = "tabs",
            Tabs =
            {
                new Tab
                {
                    Title = "Tab 1",
                    TabContent = TextTemplate("Content 1")
                },
                new Tab
                {
                    Title = "Lazy A",
                    LazyLoadContent = true,
                    TabContent = TextTemplate("Lazy A Content")
                },
                new Tab
                {
                    Title = "Lazy B",
                    LazyLoadContent = true,
                    TabContent = TextTemplate("Lazy B Content")
                }
            }
        });

        // Initially no lazy content rendered
        var html = await result.Browser.GetHtmlAsync();
        Assert.DoesNotContain("Lazy A Content", html);
        Assert.DoesNotContain("Lazy B Content", html);

        // Activate Lazy A (postback)
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await tabButtons[1].ClickAsync();

        html = await result.Browser.GetHtmlAsync();
        Assert.Contains("Lazy A Content", html);

        // Activate Lazy B (second postback)
        tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await tabButtons[2].ClickAsync();

        // Lazy B should be visible AND Lazy A content should still be in the DOM
        html = await result.Browser.GetHtmlAsync();
        Assert.Contains("Lazy B Content", html);
        Assert.Contains("Lazy A Content", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task MultiControlLazyTabPersistsAfterOtherControlPostback(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            Controls =
            [
                new TabControl
                {
                    ID = "tabsA",
                    Tabs =
                    {
                        new Tab { Title = "A-Tab 1", TabContent = TextTemplate("A Content 1") },
                        new Tab { Title = "A-Lazy", LazyLoadContent = true, TabContent = TextTemplate("A Lazy Content") }
                    }
                },
                new TabControl
                {
                    ID = "tabsB",
                    Tabs =
                    {
                        new Tab { Title = "B-Tab 1", TabContent = TextTemplate("B Content 1") },
                        new Tab { Title = "B-Lazy", LazyLoadContent = true, TabContent = TextTemplate("B Lazy Content") }
                    }
                }
            ]
        });

        // Activate lazy tab in tabsA (postback)
        var allButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        Assert.Equal(4, allButtons.Count);
        await allButtons[1].ClickAsync(); // A-Lazy

        var html = await result.Browser.GetHtmlAsync();
        Assert.Contains("A Lazy Content", html);

        // Trigger postback from tabsB (click B-Lazy)
        allButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await allButtons[3].ClickAsync(); // B-Lazy

        // A Lazy Content should still be in the DOM after the second postback
        html = await result.Browser.GetHtmlAsync();
        Assert.Contains("B Lazy Content", html);
        Assert.Contains("A Lazy Content", html);
    }

    /// <summary>
    /// Waits for the lazy loader's auto-postback to complete by polling for the
    /// data-wfc-lazy attribute to become empty (loaded state).
    /// </summary>
    private static async Task WaitForLazyLoadAsync(ITestContext browser, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                // When loaded, data-wfc-lazy has an empty value
                if (browser.QuerySelector("[data-wfc-lazy]:not([data-wfc-lazy=''])") is null)
                    return;
            }
            catch
            {
                // Page may be reloading during navigation
            }

            await Task.Delay(100);
        }
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task LazyTabPostbackKeepsLoadedContent(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            var labelA = new Ref<Label>();
            var labelB = new Ref<Label>();

            return new Panel()
            {
                Controls =
                [
                    new Button
                    {
                        Text = "Trigger Postback",
                        CssClass = "postback-btn",
                    },
                    new TabControl
                    {
                        ID = "tabs",
                        Tabs =
                        {
                            new Tab
                            {
                                Title = "Lazy A",
                                TabContent = new InlineTemplate(c =>
                                {
                                    c.Controls.AddWithoutPageEvents(new Label
                                    {
                                        Ref = labelA,
                                        Text = "A:Initial",
                                        CssClass = "lazy-a-label"
                                    });
                                    c.Controls.AddWithoutPageEvents(new Button
                                    {
                                        Text = "Update",
                                        CssClass = "lazy-a-submit",
                                        OnClick = (_, _) => labelA.Value.Text = "A:Clicked"
                                    });
                                })
                            },
                            new Tab
                            {
                                Title = "Lazy B",
                                LazyLoadContent = true,
                                TabContent = new InlineTemplate(c =>
                                {
                                    c.Controls.AddWithoutPageEvents(new Label
                                    {
                                        Ref = labelB,
                                        Text = "B:Initial",
                                        CssClass = "lazy-b-label"
                                    });
                                    c.Controls.AddWithoutPageEvents(new Button
                                    {
                                        Text = "Update",
                                        CssClass = "lazy-b-submit",
                                        OnClick = (_, _) => labelB.Value.Text = "B:Clicked"
                                    });
                                })
                            }
                        }
                    }
                ]
            };
        });

        await WaitForLazyLoadAsync(result.Browser);

        Assert.Equal("A:Initial", result.Browser.QuerySelector(".lazy-a-label")?.Text);
        await result.Browser.QuerySelector(".lazy-a-submit")!.ClickAsync();
        Assert.Equal("A:Clicked", result.Browser.QuerySelector(".lazy-a-label")?.Text);

        await result.Browser.QuerySelector("[data-wfc-tab-index='1']")!.ClickAsync();
        await WaitForLazyLoadAsync(result.Browser);

        Assert.Equal("B:Initial", result.Browser.QuerySelector(".lazy-b-label")?.Text);
        await result.Browser.QuerySelector(".lazy-b-submit")!.ClickAsync();
        Assert.Equal("B:Clicked", result.Browser.QuerySelector(".lazy-b-label")?.Text);

        await result.Browser.QuerySelector(".postback-btn")!.ClickAsync();
        Assert.Equal("B:Clicked", result.Browser.QuerySelector(".lazy-b-label")?.Text);

        await result.Browser.QuerySelector("[data-wfc-tab-index='0']")!.ClickAsync();
        Assert.Equal("A:Clicked", result.Browser.QuerySelector(".lazy-a-label")?.Text);

        await result.Browser.QuerySelector(".postback-btn")!.ClickAsync();
        Assert.Equal("A:Clicked", result.Browser.QuerySelector(".lazy-a-label")?.Text);

        await result.Browser.QuerySelector("[data-wfc-tab-index='1']")!.ClickAsync();
        Assert.Equal("B:Clicked", result.Browser.QuerySelector(".lazy-b-label")?.Text);

        await result.Browser.QuerySelector(".postback-btn")!.ClickAsync();
        Assert.Equal("B:Clicked", result.Browser.QuerySelector(".lazy-b-label")?.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task MultiControlClientSideSwitchPreservedOnPostback(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            Controls =
            [
                new TabControl
                {
                    ID = "tabsA",
                    Tabs =
                    {
                        new Tab { Title = "A-Tab 1", TabContent = TextTemplate("A Content 1") },
                        new Tab { Title = "A-Tab 2", TabContent = TextTemplate("A Content 2") }
                    }
                },
                new TabControl
                {
                    ID = "tabsB",
                    Tabs =
                    {
                        new Tab { Title = "B-Tab 1", TabContent = TextTemplate("B Content 1") },
                        new Tab { Title = "B-Lazy", LazyLoadContent = true, TabContent = TextTemplate("B Lazy Content") }
                    }
                }
            ]
        });

        // Switch to A-Tab 2 (client-side, no postback)
        var allButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        Assert.Equal(4, allButtons.Count);
        await allButtons[1].ClickAsync(); // A-Tab 2

        // Verify A-Tab 2 is selected
        allButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        Assert.Equal("true", await allButtons[1].GetAttributeAsync("aria-selected"));

        // Trigger postback from tabsB (click B-Lazy)
        await allButtons[3].ClickAsync(); // B-Lazy

        // After postback: A-Tab 2 should still be selected
        allButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        Assert.Equal("false", await allButtons[0].GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await allButtons[1].GetAttributeAsync("aria-selected"));

        // B-Lazy should be active and loaded
        Assert.Equal("true", await allButtons[3].GetAttributeAsync("aria-selected"));
        var html = await result.Browser.GetHtmlAsync();
        Assert.Contains("B Lazy Content", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ContentLoadedEventFires(Browser type)
    {
        var eventFired = false;

        await using var result = await fixture.StartAsync(type, () =>
        {
            var lazyTab = new Tab
            {
                Title = "Lazy Tab",
                LazyLoadContent = true,
                TabContent = TextTemplate("Lazy Content")
            };

            lazyTab.ContentLoaded += (_, _) =>
            {
                eventFired = true;
                return Task.CompletedTask;
            };

            return new TabControl
            {
                ID = "tabs",
                Tabs =
                {
                    new Tab
                    {
                        Title = "Tab 1",
                        TabContent = TextTemplate("Content 1")
                    },
                    lazyTab
                }
            };
        });

        // Event should not have fired yet
        Assert.False(eventFired);

        // Activate the lazy tab (scoped postback)
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await tabButtons[1].ClickAsync();

        // ContentLoaded should have fired and lazy content should be visible
        Assert.True(eventFired);
        Assert.Contains("Lazy Content", await result.Browser.GetHtmlAsync());
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenContentLoadedModifiesLabelThenRendersUpdatedText(Browser type)
    {
        Ref<Label>? label = null;

        await using var result = await fixture.StartAsync(type, () =>
        {
            label = new Ref<Label>();

            var lazyTab = new Tab
            {
                Title = "Lazy Tab",
                LazyLoadContent = true,
                TabContent = new InlineTemplate(c =>
                {
                    // Mimic ASPX-generated code: inner text becomes a child Literal
                    // e.g. <wfc:Label ID="lblResult">Loading...</wfc:Label>
                    var lbl = new Label { Ref = label, ID = "lblResult" };
                    lbl.AddParsedSubObject(new LiteralControl { Text = "Loading..." });
                    c.Controls.AddWithoutPageEvents(lbl);
                })
            };

            lazyTab.ContentLoaded += (_, _) =>
            {
                label!.Value.Text = "Hello world! This content was loaded on demand.";
                return Task.CompletedTask;
            };

            return new TabControl
            {
                ID = "tabs",
                Tabs =
                {
                    new Tab
                    {
                        Title = "Tab 1",
                        TabContent = TextTemplate("Content 1")
                    },
                    lazyTab
                }
            };
        });

        // Initially, lazy tab content should not be present
        var html = await result.Browser.GetHtmlAsync();
        Assert.DoesNotContain("Hello world!", html);

        // Activate the lazy tab (scoped postback)
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await tabButtons[1].ClickAsync();

        // After postback, the label should show the updated text from ContentLoaded
        html = await result.Browser.GetHtmlAsync();
        Assert.DoesNotContain("Loading...", html);
        Assert.Contains("Hello world! This content was loaded on demand.", html);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task AutoPostBackTriggersFullPostback(Browser type)
    {
        var eventFired = false;

        await using var result = await fixture.StartAsync(type, () =>
        {
            var label = new Ref<Label>();

            var tabControl = new TabControl
            {
                ID = "tabs",
                Tabs =
                {
                    new Tab
                    {
                        Title = "Tab 1",
                        TabContent = new InlineTemplate(c =>
                            c.Controls.AddWithoutPageEvents(new Label { Ref = label, Text = "Initial" }))
                    },
                    new Tab
                    {
                        Title = "Tab 2",
                        AutoPostBack = true,
                        TabContent = TextTemplate("Content 2")
                    }
                }
            };

            tabControl.ActiveTabChanged += (_, _) =>
            {
                eventFired = true;
                label.Value.Text = "Changed";
                return Task.CompletedTask;
            };

            return tabControl;
        });

        // Click Tab 2 (AutoPostBack triggers full postback)
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await tabButtons[1].ClickAsync();

        // ActiveTabChanged should have fired via the full postback
        Assert.True(eventFired);
        Assert.Contains("Changed", await result.Browser.GetHtmlAsync());
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ActiveTabChanged_TabControlAutoPostBackTrue_FiresImmediately(Browser type)
    {
        var eventFired = false;

        await using var result = await fixture.StartAsync(type, () =>
        {
            var label = new Ref<Label>();

            var tabControl = new TabControl
            {
                ID = "tabs",
                AutoPostBack = true,
                Tabs =
                {
                    new Tab
                    {
                        Title = "Tab 1",
                        TabContent = new InlineTemplate(c =>
                            c.Controls.AddWithoutPageEvents(new Label { Ref = label, Text = "Initial" }))
                    },
                    new Tab
                    {
                        Title = "Tab 2",
                        TabContent = TextTemplate("Content 2")
                    }
                }
            };

            tabControl.ActiveTabChanged += (_, _) =>
            {
                eventFired = true;
                label.Value.Text = "Changed";
                return Task.CompletedTask;
            };

            return tabControl;
        });

        // Click Tab 2 — TabControl.AutoPostBack causes full postback
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await tabButtons[1].ClickAsync();

        // ActiveTabChanged should have fired immediately via the postback
        Assert.True(eventFired);
        Assert.Contains("Changed", await result.Browser.GetHtmlAsync());
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ActiveTabChanged_TabControlAutoPostBackFalse_RequiresExternalPostback(Browser type)
    {
        var eventFired = false;

        await using var result = await fixture.StartAsync(type, () =>
        {
            var label = new Ref<Label>();
            var button = new Button { ID = "postbackBtn", Text = "Submit" };

            var tabControl = new TabControl
            {
                ID = "tabs",
                AutoPostBack = false,
                Tabs =
                {
                    new Tab
                    {
                        Title = "Tab 1",
                        TabContent = new InlineTemplate(c =>
                            c.Controls.AddWithoutPageEvents(new Label { Ref = label, Text = "Initial" }))
                    },
                    new Tab
                    {
                        Title = "Tab 2",
                        TabContent = TextTemplate("Content 2")
                    }
                }
            };

            tabControl.ActiveTabChanged += (_, _) =>
            {
                eventFired = true;
                label.Value.Text = "Changed";
                return Task.CompletedTask;
            };

            return new Panel { Controls = [tabControl, button] };
        });

        // Click Tab 2 — client-side switch only, no postback
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await tabButtons[1].ClickAsync();

        // Event should NOT have fired yet (no postback)
        Assert.False(eventFired);

        // Trigger external postback via button
        var submitBtn = result.Browser.QuerySelector("#postbackBtn");
        Assert.NotNull(submitBtn);
        await submitBtn.ClickAsync();

        // Now ActiveTabChanged should have fired
        Assert.True(eventFired);
        Assert.Contains("Changed", await result.Browser.GetHtmlAsync());
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ActiveTabChanged_TabAutoPostBackTrue_FiresImmediately(Browser type)
    {
        var eventFired = false;

        await using var result = await fixture.StartAsync(type, () =>
        {
            var label = new Ref<Label>();

            return new TabControl
            {
                ID = "tabs",
                Tabs =
                {
                    new Tab
                    {
                        Title = "Tab 1",
                        TabContent = Template(() =>
                        [
                            new Label
                            {
                                Ref = label,
                                Text = "Initial"
                            }
                        ])
                    },
                    new Tab
                    {
                        Title = "Tab 2",
                        AutoPostBack = true,
                        TabContent = TextTemplate("Content 2")
                    }
                },
                OnActiveTabChanged = (_, _) =>
                {
                    eventFired = true;
                    label.Value.Text = "Changed";
                }
            };
        });

        // Click Tab 2 — Tab.AutoPostBack causes full postback
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await tabButtons[1].ClickAsync();

        // ActiveTabChanged should have fired immediately
        Assert.True(eventFired);
        Assert.Contains("Changed", await result.Browser.GetHtmlAsync());
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ActiveTabChanged_TabAutoPostBackFalse_RequiresExternalPostback(Browser type)
    {
        var eventFired = false;

        await using var result = await fixture.StartAsync(type, () =>
        {
            var label = new Ref<Label>();
            var button = new Button { ID = "postbackBtn", Text = "Submit" };

            var tabControl = new TabControl
            {
                ID = "tabs",
                Tabs =
                {
                    new Tab
                    {
                        Title = "Tab 1",
                        TabContent = new InlineTemplate(c =>
                            c.Controls.AddWithoutPageEvents(new Label { Ref = label, Text = "Initial" }))
                    },
                    new Tab
                    {
                        Title = "Tab 2",
                        AutoPostBack = false,
                        TabContent = TextTemplate("Content 2")
                    }
                }
            };

            tabControl.ActiveTabChanged += (_, _) =>
            {
                eventFired = true;
                label.Value.Text = "Changed";
                return Task.CompletedTask;
            };

            return new Panel { Controls = [tabControl, button] };
        });

        // Click Tab 2 — client-side switch only (AutoPostBack=false)
        var tabButtons = await result.Browser.QuerySelectorAll("[role=tab]").ToListAsync();
        await tabButtons[1].ClickAsync();

        // Event should NOT have fired yet
        Assert.False(eventFired);

        // Trigger external postback via button
        var submitBtn = result.Browser.QuerySelector("#postbackBtn");
        Assert.NotNull(submitBtn);
        await submitBtn.ClickAsync();

        // Now ActiveTabChanged should have fired
        Assert.True(eventFired);
        Assert.Contains("Changed", await result.Browser.GetHtmlAsync());
    }
}
