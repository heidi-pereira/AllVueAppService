import React from 'react';
import { INavStep } from "./ProductTour";
import './ProductTour.less';

export class ProductTourDefinitions {
    static steps: { [path: string]: INavStep[] } = {
        "Tour1-EO-UK": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Find the net buzz reports',
                content: <div className="productTourStep">
                    <p>Lets start by looking into the buzz around a brand.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined float-start me-2">keyboard</i> Type "buzz" into the search bar</p >
                    <p>You can see a number of buzz questions in the list, but you are interested in "Net Buzz".</p>
                    <p className="action"><i className="material-symbols-outlined float-start me-2">touch_app</i> Click on "Net Buzz" in the list to go to the relevant charts</p>
                </div>,
                placement: "right"
            },
            {
                url: "/brand-attention/net-buzz/over-time",
                target: '.averageSelector',
                title: 'The over time view',
                content: <div className="productTourStep">
                    <p>Net buzz is influenced by current affairs and is predictive of the future performance of a brand.</p>
                    <p>In this first view (the over time view) you can see how net buzz tracks over time compared to your key competitors.</p>
                    <p>Let&apos;s focus on this quarter and the previous quarter.</p>
                    <p className="action"><i className="material-symbols-outlined float-start me-2">touch_app</i> Set your view to &apos;Quarterly&apos; using the average dropdown</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-attention/net-buzz/over-time?Average=Quarterly",
                target: '#viewSelectorToggle',
                title: 'Changing chart type',
                content: <div className="productTourStep">
                    <p>Now you can see there are fewer data points and the data has become more stable. The data has been aggregated up to an average of the quarters.</p>
                    <p>You want to focus on this quarter vs the last, so lets cut out the noise.</p>
                    <p className="action"><i className="material-symbols-outlined float-start me-2">bar_chart</i>Select the competition view.</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-attention/net-buzz/competition?Average=Quarterly&Competitors=All",
                target: '.filterPopup',
                title: 'Filtering the results',
                content: <div className="productTourStep">
                    <p>Let&apos;s take a look at the London market.</p>
                    <p className="action"><i className="material-symbols-outlined float-start me-2">tune</i> Go to filters, check the London Region and then hit the Apply filters button</p>
                </div>,
                placement: 'bottom'
            },
            {
                url: "/brand-attention/net-buzz/competition?Average=Quarterly&Competitors=All&Region=L",
                target: '.saveChart',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>The chart is now ready to export and share around.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'bottom'
            }
        ],
        "Tour2-EO-UK": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Investigating conversion',
                content: <div className="productTourStep">
                    <p>Before you can look at how many people are becoming users, you need to know how much of the market you are reaching.</p>
                    <p>Let&apos;s look at the awareness metric.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "awareness" into the search bar</p >
                    <p>You can see a number of awareness questions in the list, but you are interested in <b>"Brand Health &gt; Awareness".</b></p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on 'Awareness' in the Brand Health section of the list</p>
                </div >,
                placement: "right"
            },
            {
                url: "/brand-health/awareness/over-time",
                target: '#viewSelectorToggle',
                title: 'The over time view',
                content: <div className="productTourStep">
                    <p>Brand awareness is a stable metric - especially for established brands.</p>
                    <p>This is illustrated in the ranking view.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">format_list_numbered</i> Go to the ranking view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/awareness/ranking",
                target: '.averageSelector',
                title: 'Changing period',
                content: <div className="productTourStep">
                    <p>Here you can see all of the brands next to one another and their position within the core competitor set.</p>
                    <p>This metric is normally stable and therefore should be viewed over longer periods of time.</p>
                    <p>Lets take a look at how the rankings change over a quarter.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Select the quarterly option from the first dropdown menu</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/awareness/ranking?Average=Quarterly",
                target: '.navSearch>.rbt',
                title: 'Consideration',
                content: <div className="productTourStep">
                    <p>You have seen how much of the market you have reached, and where you are relative to your core competitors.</p>
                    <p>Lets now take a look at how much of the market would consider using your brand.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "consider" into the search bar</p >
                    <p>You are interested in <b>"Brand Health &gt; Consideration".</b></p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on 'Consideration' in the Brand Health section of the list</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/consideration/ranking?Average=Quarterly&Competitors=All",
                target: '#viewSelectorToggle',
                title: 'Considerers',
                content: <div className="productTourStep">
                    <p>Lets focus on the profile of the people considering using us.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">people</i> Select the profile view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/consideration/profile?Average=Quarterly&Competitors=All",
                target: '.saveChart',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>You can see how consideration for your brand changes across different demographic segments.</p>
                    <p>This is a good starting point for building hypotheses about how best to target your campaigns to convert the highest proportion of considerers into customers.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'right'
            }
        ],
        "Tour3-EO-UK": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Find the penetration reports',
                content: <div className="productTourStep">
                    <p>You want to understand who your typical customer is, and who your competitors customer are. This is useful because you can start to validate whether you are competing with other brands for the same customers.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "penetration" into the search bar</p >
                    <p>First you need to focus on people that have purchased from you in the last 12 months (L12M) - these are your active customers.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on "Penetration (L12M)" in the list to go to the relevant charts</p>
                </div >,
                placement: "right"
            },
            {
                url: "/demand-and-usage/penetration-l12m/over-time",
                target: '#viewSelectorToggle',
                title: 'Profile of your customer',
                content: <div className="productTourStep">
                    <p>You want to understand more about who your customer is.</p>
                    <p>Let's focus on the profile of the people that have used you in the last year.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">people</i> Select the profile view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/penetration-l12m/profile",
                target: '.averageSelector',
                title: 'Selecting a time period',
                content: <div className="productTourStep">
                    <p>The current time period is a little short for looking at a profile.</p>
                    <p>Let’s take a look at a larger period and focus more on seasonal differences.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Select the quarterly option from the first dropdown menu</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/penetration-l12m/profile?Average=Quarterly",
                target: '.periodsSelector',
                title: 'Competitors customers',
                content: <div className="productTourStep">
                    <p>You can now see how you have performed this quarter versus the last quarter.</p>
                    <p>You can now see whether or not your customer profile changes period to period, and start to think whether the strategy will need to differ throughout the year.</p>
                    <p>Now you have an understanding of who your customers are and whether this changes, let’s take a look at your competitors customer and how these differ.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Change “Current and previous period” to “Current period”</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/penetration-l12m/profile?Average=Quarterly&Period=Current",
                target: '.saveChart',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>You can see who your competitors main customers are and whether or not you are competing for customers from the same pool.</p>
                    <p>This gives us a general understanding of who their proposition appeals to.</p>
                    <p>This has given us an overview in to your customers, and the customers your competitors. You can do more analysis on these customer bases by using the dashboard to further investigate.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'right'
            }
        ],
        "Tour1-EO-US": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Find the net buzz reports',
                content: <div className="productTourStep">
                    <p>Lets start by looking into the buzz around a brand.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "buzz" into the search bar</p >
                    <p>You can see a number of buzz questions in the list, but you are interested in "Net Buzz".</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on "Net Buzz" in the list to go to the relevant charts</p>
                </div >,
                placement: "right"
            },
            {
                url: "/brand-attention/net-buzz/over-time",
                target: '.averageSelector',
                title: 'The over time view',
                content: <div className="productTourStep">
                    <p>Net buzz is influenced by current affairs and is predictive of the future performance of a brand.</p>
                    <p>In this first view (the over time view) you can see how net buzz tracks over time compared to your key competitors.</p>
                    <p>Let&apos;s focus on this quarter and the previous quarter.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Set your view to &apos;Quarterly&apos; using the average dropdown</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-attention/net-buzz/over-time?Average=Quarterly",
                target: '#viewSelectorToggle',
                title: 'Changing chart type',
                content: <div className="productTourStep">
                    <p>Now you can see there are fewer data points and the data has become more stable. The data has been aggregated up to an average of the quarters.</p>
                    <p>You want to focus on this quarter vs the last, so lets cut out the noise.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">bar_chart</i>Select the competition view.</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-attention/net-buzz/competition?Average=Quarterly&Competitors=All",
                target: '.filterPopup',
                title: 'Filtering the results',
                content: <div className="productTourStep">
                    <p>Let&apos;s take a look at the Mid-west market.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">tune</i> Go to filters, check the Mid-west Region and then hit the Apply filters button</p>
                </div>,
                placement: 'bottom'
            },
            {
                url: "/brand-attention/net-buzz/competition?Average=Quarterly&Competitors=All&Region=M",
                target: '.saveChart',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>The chart is now ready to export and share around.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'bottom'
            }
        ],
        "Tour2-EO-US": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Investigating conversion',
                content: <div className="productTourStep">
                    <p>Before you can look at how many people are becoming users, you need to know how much of the market you are reaching.</p>
                    <p>Let&apos;s look at the awareness metric.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "awareness" into the search bar</p >
                    <p>You can see a number of awareness questions in the list, but you are interested in <b>"Brand Health &gt; Awareness".</b></p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on 'Awareness' in the Brand Health section of the list</p>
                </div >,
                placement: "right"
            },
            {
                url: "/brand-health/awareness/over-time",
                target: '#viewSelectorToggle',
                title: 'The over time view',
                content: <div className="productTourStep">
                    <p>Brand awareness is a stable metric - especially for established brands.</p>
                    <p>This is illustrated in the ranking view.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">format_list_numbered</i> Go to the ranking view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/awareness/ranking",
                target: '.averageSelector',
                title: 'Changing period',
                content: <div className="productTourStep">
                    <p>Here you can see all of the brands next to one another and their position within the core competitor set.</p>
                    <p>This metric is normally stable and therefore should be viewed over longer periods of time.</p>
                    <p>Lets take a look at how the rankings change over a quarter.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Select the quarterly option from the first dropdown menu</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/awareness/ranking?Average=Quarterly",
                target: '.navItem-Consideration',
                title: 'Consideration',
                content: <div className="productTourStep">
                    <p>You have seen how much of the market you have reached, and where you are relative to your core competitors.</p>
                    <p>Lets now take a look at how much of the market would consider using your brand.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Go to consideration</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/consideration/ranking?Average=Quarterly&Competitors=All",
                target: '#viewSelectorToggle',
                title: 'Considerers',
                content: <div className="productTourStep">
                    <p>Lets focus on the profile of the people considering using us.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">people</i> Select the profile view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/consideration/profile?Average=Quarterly&Competitors=All",
                target: '.saveChart',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>You can see how consideration for your brand changes across different demographic segments.</p>
                    <p>This is a good starting point for building hypotheses about how best to target your campaigns to convert the highest proportion of considerers into customers.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'right'
            }
        ],
        "Tour3-EO-US": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Find the penetration reports',
                content: <div className="productTourStep">
                    <p>You want to understand who your typical customer is, and who your competitors customer are. This is useful because you can start to validate whether you are competing with other brands for the same customers.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "penetration" into the search bar</p >
                    <p>First you need to focus on people that have purchased from you in the last 12 months (L12M) - these are your active customers.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on "Penetration (L12M)" in the list to go to the relevant charts</p>
                </div >,
                placement: "right"
            },
            {
                url: "/demand-and-usage/penetration-l12m/over-time",
                target: '#viewSelectorToggle',
                title: 'Profile of your customer',
                content: <div className="productTourStep">
                    <p>You want to understand more about who your customer is.</p>
                    <p>Let's focus on the profile of the people that have used you in the last year.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">people</i> Select the profile view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/penetration-l12m/profile",
                target: '.averageSelector',
                title: 'Selecting a time period',
                content: <div className="productTourStep">
                    <p>The current time period is a little short for looking at a profile.</p>
                    <p>Let’s take a look at a larger period and focus more on seasonal differences.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Select the quarterly option from the first dropdown menu</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/penetration-l12m/profile?Average=Quarterly",
                target: '.periodsSelector',
                title: 'Competitors customers',
                content: <div className="productTourStep">
                    <p>You can now see how you have performed this quarter versus the last quarter.</p>
                    <p>You can now see whether or not your customer profile changes period to period, and start to think whether the strategy will need to differ throughout the year.</p>
                    <p>Now you have an understanding of who your customers are and whether this changes, let’s take a look at your competitors customer and how these differ.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Change “Current and previous period” to “Current period”</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/penetration-l12m/profile?Average=Quarterly&Period=Current",
                target: '.saveChart',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>You can see who your competitors main customers are and whether or not you are competing for customers from the same pool.</p>
                    <p>This gives us a general understanding of who their proposition appeals to.</p>
                    <p>This has given us an overview in to your customers, and the customers your competitors. You can do more analysis on these customer bases by using the dashboard to further investigate.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'right'
            }
        ],

        // CHARITIES TOURS:

        "Tour1-CH-UK": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Find the net buzz reports',
                content: <div className="productTourStep">
                    <p>Lets start by looking into the buzz around a charity.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "buzz" into the search bar</p >
                    <p>You can see a number of buzz questions in the list, but you are interested in "Buzz - Net buzz".</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on "Buzz - Net buzz" in the list to go to the relevant charts</p>
                </div >,
                placement: "right"
            },
            {
                url: "/brand-attention/buzz/net-buzz/over-time",
                target: '.averageSelector',
                title: 'The over time view',
                content: <div className="productTourStep">
                    <p>Net buzz is influenced by current affairs and is predictive of the future performance of a brand.</p>
                    <p>In this first view (the over time view) you can see how net buzz tracks over time compared to your key competitors.</p>
                    <p>Let&apos;s focus on this quarter and the previous quarter.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Set your view to &apos;Quarterly&apos; using the average dropdown</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-attention/buzz/net-buzz/over-time?Average=Quarterly",
                target: '#viewSelectorToggle',
                title: 'Changing chart type',
                content: <div className="productTourStep">
                    <p>Now you can see there are fewer data points and the data has become more stable. The data has been aggregated up to an average of the quarters.</p>
                    <p>You want to focus on this quarter vs the last, so lets cut out the noise.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">bar_chart</i>Select the competition view.</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-attention/buzz/net-buzz/competition?Average=Quarterly&Competitors=All",
                target: '.filterPopup',
                title: 'Filtering the results',
                content: <div className="productTourStep">
                    <p>Let&apos;s take a look at the London market.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">tune</i> Go to filters, check the London Region and then hit the Apply filters button</p>
                </div>,
                placement: 'bottom'
            },
            {
                url: "/brand-attention/buzz/net-buzz/competition?Average=Quarterly&Competitors=All&Region=L",
                target: '.saveChart',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>The chart is now ready to export and share around.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'bottom'
            }
        ],
        "Tour2-CH-UK": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Investigating conversion',
                content: <div className="productTourStep">
                    <p>Before you can look at how many people are becoming donors, you need to know how much of the sector you are reaching.</p>
                    <p>Let&apos;s look at the awareness metric.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "awareness" into the search bar</p >
                    <p>You can see a number of awareness questions in the list, but you are interested in <b>"Brand Health &gt; Awareness".</b> This can be found at the bottom of the list.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on 'Awareness' in the Brand Health section of the list</p>
                </div >,
                placement: "right"
            },
            {
                url: "/brand-health/awareness/over-time",
                target: '#viewSelectorToggle',
                title: 'The over time view',
                content: <div className="productTourStep">
                    <p>Brand awareness is a stable metric - especially for established brands.</p>
                    <p>This is illustrated in the ranking view.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">format_list_numbered</i> Go to the ranking view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/awareness/ranking",
                target: '.averageSelector',
                title: 'Changing period',
                content: <div className="productTourStep">
                    <p>Here you can see all of the charities next to one another and their position within the core competitor set.</p>
                    <p>This metric is normally stable and therefore should be viewed over longer periods of time.</p>
                    <p>Lets take a look at how the rankings change over a quarter.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Select the quarterly option from the first dropdown menu</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/awareness/ranking?Average=Quarterly",
                target: '.navSearch>.rbt',
                title: 'Consideration',
                content: <div className="productTourStep">
                    <p>You have seen how much of the sector you have reached, and where you are relative to your core competitors.</p>
                    <p>Lets now take a look at how much of the population would consider donating to your charity.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "Consideration" into the search bar, and go to the "Consideration" page</p >
                </div>,
                placement: 'right'
            },
            {
                url: "/supporter-journey/consideration/ranking?Average=Quarterly&Competitors=All",
                target: '#viewSelectorToggle',
                title: 'Considerers',
                content: <div className="productTourStep">
                    <p>Lets focus on the profile of the people considering using us.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">people</i> Select the profile view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/supporter-journey/consideration/profile?Average=Quarterly&Competitors=All",
                target: '.saveChart',
                title: 'Profiles',
                content: <div className="productTourStep">
                    <p>You can see how consideration for your charity changes across different demographic segments.</p>
                    <p>This is a good starting point for building hypotheses about how best to target your campaigns to convert the highest proportion of considerers into supporters.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'right'
            }
        ],
        "Tour3-CH-UK": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Find the penetration reports',
                content: <div className="productTourStep">
                    <p>You want to understand who your typical supporter is, and who your competitors supporters are. This is useful because you can start to validate whether you are competing with other charities for the same supporters.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "support" into the search bar</p >
                    <p>First you need to focus on people that have supported your charity in the last 12 months (L12M) - these are your active supporters.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on "Support (L12M)" in the list to go to the relevant charts</p>
                </div >,
                placement: "right"
            },
            {
                url: "/supporter-journey/support-l12m/over-time",
                target: '#viewSelectorToggle',
                title: 'Profile of your supporter',
                content: <div className="productTourStep">
                    <p>You want to understand more about who your supporter is.</p>
                    <p>Let's focus on the profile of the people that have used you in the last year.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">people</i> Select the profile view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/supporter-journey/support-l12m/profile",
                target: '.averageSelector',
                title: 'Selecting a time period',
                content: <div className="productTourStep">
                    <p>The current time period is a little short for looking at a profile.</p>
                    <p>Let’s take a look at a larger period and focus more on seasonal differences.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Select the quarterly option from the first dropdown menu</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/supporter-journey/support-l12m/profile?Average=Quarterly",
                target: '.periodsSelector',
                title: 'Competitors supporters',
                content: <div className="productTourStep">
                    <p>You can now see how you have performed this quarter versus the last quarter.</p>
                    <p>You can now see whether or not your supporter profile changes period to period, and start to think whether the strategy will need to differ throughout the year.</p>
                    <p>Now you have an understanding of who your supporters are and whether this changes, let’s take a look at your competitors supporter and how these differ.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Change “Current and previous period” to “Current period”</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/supporter-journey/support-l12m/profile?Average=Quarterly&Period=Current",
                target: '.periodsSelector',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>You can see who your competitors main supporters are and whether or not you are competing for supporters from the same pool.</p>
                    <p>This gives us a general understanding of who they appeal to.</p>
                    <p>This has given us an overview in to your supporters, and the supporters your competitors. You can do more analysis on these supporter bases by using the dashboard to further investigate.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'right'
            }
        ],

        // FINANCIAL SERVICES TOURS:

        "Tour1-FS-UK-All": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Find the net buzz reports',
                content: <div className="productTourStep">
                    <p>Lets start by looking into the buzz around a brand.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "buzz" into the search bar</p >
                    <p>You can see a number of buzz questions in the list, but you are interested in "Buzz - Net Buzz".</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on "Buzz - Net Buzz" in the list to go to the relevant charts</p>
                </div >,
                placement: "right"
            },
            {
                url: "/brand-attention/buzz/net-buzz/over-time",
                target: '.averageSelector',
                title: 'The over time view',
                content: <div className="productTourStep">
                    <p>Net buzz is influenced by current affairs and is predictive of the future performance of a brand.</p>
                    <p>In this first view (the over time view) you can see how net buzz tracks over time compared to your key competitors.</p>
                    <p>Let&apos;s focus on this quarter and the previous quarter.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Set your view to &apos;Quarterly&apos; using the average dropdown</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-attention/buzz/net-buzz/over-time?Average=Quarterly",
                target: '#viewSelectorToggle',
                title: 'Changing chart type',
                content: <div className="productTourStep">
                    <p>Now you can see there are fewer data points and the data has become more stable. The data has been aggregated up to an average of the quarters.</p>
                    <p>You want to focus on this quarter vs the last, so lets cut out the noise.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">bar_chart</i> Select the competition view.</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-attention/buzz/net-buzz/competition?Average=Quarterly&Competitors=All",
                target: '.filterPopup',
                title: 'Filtering the results',
                content: <div className="productTourStep">
                    <p>Let&apos;s take a look at the London market.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">tune</i> Go to filters, check the London Region and then hit the Apply filters button</p>
                </div>,
                placement: 'bottom'
            },
            {
                url: "/brand-attention/buzz/net-buzz/competition?Average=Quarterly&Competitors=All&Region=L",
                target: '.saveChart',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>The chart is now ready to export and share around.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'bottom'
            }
        ],
        "Tour2-FS-UK-All": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Investigating conversion',
                content: <div className="productTourStep">
                    <p>Before you can look at how many people are becoming users, you need to know how much of the market you are reaching.</p>
                    <p>Let&apos;s look at the awareness metric.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "awareness" into the search bar</p >
                    <p>You can see a number of awareness questions in the list, but you are interested in <b>"Brand Health &gt; Awareness".</b></p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on 'Awareness' in the Brand Health section of the list</p>
                </div >,
                placement: "right"
            },
            {
                url: "/brand-health/awareness/over-time",
                target: '#viewSelectorToggle',
                title: 'The over time view',
                content: <div className="productTourStep">
                    <p>Brand awareness is a stable metric - especially for established brands.</p>
                    <p>This is illustrated in the ranking view.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">format_list_numbered</i> Go to the ranking view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/awareness/ranking",
                target: '.averageSelector',
                title: 'Changing period',
                content: <div className="productTourStep">
                    <p>Here you can see all of the brands next to one another and their position within the core competitor set.</p>
                    <p>This metric is normally stable and therefore should be viewed over longer periods of time.</p>
                    <p>Lets take a look at how the rankings change over a quarter.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Select the quarterly option from the first dropdown menu</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/awareness/ranking?Average=Quarterly",
                target: '.navSearch>.rbt',
                title: 'Brand Consideration',
                content: <div className="productTourStep">
                    <p>You have seen how much of the market you have reached, and where you are relative to your core competitors.</p>
                    <p>Lets now take a look at how much of the market would consider using your brand.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "consider" into the search bar</p >
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on 'Brand Consideration' in the Demand and Usage section of the list</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/brand-consideration/over-time?Average=Quarterly",
                target: '#viewSelectorToggle',
                title: 'Ranking table',
                content: <div className="productTourStep">
                    <p>Now you can see how consideration changes over time.</p>
                    <p>This is a stable metric so let's look at how we compare to a larger group of competitors on the ranking table.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Go to the "ranking table" view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/brand-consideration/ranking?Average=Quarterly&Competitors=All",
                target: '#viewSelectorToggle',
                title: 'Considerers',
                content: <div className="productTourStep">
                    <p>Lets focus on the profile of the people considering using us.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">people</i> Select the profile view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/brand-consideration/profile?Average=Quarterly&Competitors=All",
                target: '.saveChart',
                title: 'Profiles',
                content: <div className="productTourStep">
                    <p>You can see how consideration for your brand changes across different demographic segments.</p>
                    <p>This is a good starting point for building hypotheses about how best to target your campaigns to convert the highest proportion of considerers into customers.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'right'
            }
        ],
        "Tour3-FS-UK-All": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Find the penetration reports',
                content: <div className="productTourStep">
                    <p>You want to understand who your typical customer is, and who your competitors customer are. This is useful because you can start to validate whether you are competing with other brands for the same customers.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "penetration" into the search bar</p >
                    <p>First you need to focus on people that have purchased from your brand - these are your total active customers.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on "Brand Penetration" in the list to go to the relevant charts</p>
                </div >,
                placement: "right"
            },
            {
                url: "/demand-and-usage/brand-penetration/over-time",
                target: '#viewSelectorToggle',
                title: 'Profile of your customer',
                content: <div className="productTourStep">
                    <p>You want to understand more about who your customer is.</p>
                    <p>Let's focus on the profile of the people that have used you in the last year.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">people</i> Select the profile view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/brand-penetration/profile",
                target: '.averageSelector',
                title: 'Selecting a time period',
                content: <div className="productTourStep">
                    <p>The current time period is a little short for looking at a profile.</p>
                    <p>Let’s take a look at a larger period and focus more on seasonal differences.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Select the quarterly option from the first dropdown menu</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/brand-penetration/profile?Average=Quarterly",
                target: '.periodsSelector',
                title: 'Competitors customers',
                content: <div className="productTourStep">
                    <p>You can now see how you have performed this quarter versus the last quarter.</p>
                    <p>You can now see whether or not your customer profile changes period to period, and start to think whether the strategy will need to differ throughout the year.</p>
                    <p>Now you have an understanding of who your customers are and whether this changes, let’s take a look at your competitors customer and how these differ.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Change “Current and previous period” to “Current period”</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/brand-penetration/profile?Average=Quarterly&Period=Current",
                target: '.periodsSelector',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>You can see who your competitors main customers are and whether or not you are competing for customers from the same pool.</p>
                    <p>This gives us a general understanding of who their proposition appeals to.</p>
                    <p>This has given us an overview in to your customers, and the customers your competitors. You can do more analysis on these customer bases by using the dashboard to further investigate.</p>
                </div>,
                placement: 'right'
            }
        ],

        "Tour1-FS-UK-Sub": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Find the net buzz reports',
                content: <div className="productTourStep">
                    <p>Lets start by looking into the buzz around a brand.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "buzz" into the search bar</p >
                    <p>You can see a number of buzz questions in the list, but you are interested in "Buzz - Net Buzz".</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on "Buzz - Net Buzz" in the list to go to the relevant charts</p>
                </div >,
                placement: "right"
            },
            {
                url: "/brand-attention/buzz/net-buzz/over-time",
                target: '.averageSelector',
                title: 'The over time view',
                content: <div className="productTourStep">
                    <p>Net buzz is influenced by current affairs and is predictive of the future performance of a brand.</p>
                    <p>In this first view (the over time view) you can see how net buzz tracks over time compared to your key competitors.</p>
                    <p>Let&apos;s focus on this quarter and the previous quarter.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Set your view to &apos;Quarterly&apos; using the average dropdown</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-attention/buzz/net-buzz/over-time?Average=Quarterly",
                target: '#viewSelectorToggle',
                title: 'Changing chart type',
                content: <div className="productTourStep">
                    <p>Now you can see there are fewer data points and the data has become more stable. The data has been aggregated up to an average of the quarters.</p>
                    <p>You want to focus on this quarter vs the last, so lets cut out the noise.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">bar_chart</i> Select the competition view.</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-attention/buzz/net-buzz/competition?Average=Quarterly&Competitors=All",
                target: '.filterPopup',
                title: 'Filtering the results',
                content: <div className="productTourStep">
                    <p>Let&apos;s take a look at the London market.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">tune</i> Go to filters, check the London Region and then hit the Apply filters button</p>
                </div>,
                placement: 'bottom'
            },
            {
                url: "/brand-attention/buzz/net-buzz/competition?Average=Quarterly&Competitors=All&Region=L",
                target: '.saveChart',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>The chart is now ready to export and share around.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'bottom'
            }
        ],
        "Tour2-FS-UK-Sub": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Investigating conversion',
                content: <div className="productTourStep">
                    <p>Before you can look at how many people are becoming users, you need to know how much of the market you are reaching.</p>
                    <p>Let&apos;s look at the awareness metric.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "awareness" into the search bar</p >
                    <p>You can see a number of awareness questions in the list, but you are interested in <b>"Brand Health &gt; Awareness".</b></p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on 'Awareness' in the Brand Health section of the list</p>
                </div >,
                placement: "right"
            },
            {
                url: "/brand-health/awareness/over-time",
                target: '#viewSelectorToggle',
                title: 'The over time view',
                content: <div className="productTourStep">
                    <p>Brand awareness is a stable metric - especially for established brands.</p>
                    <p>This is illustrated in the ranking view.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">format_list_numbered</i> Go to the ranking view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/awareness/ranking",
                target: '.averageSelector',
                title: 'Changing period',
                content: <div className="productTourStep">
                    <p>Here you can see all of the brands next to one another and their position within the core competitor set.</p>
                    <p>This metric is normally stable and therefore should be viewed over longer periods of time.</p>
                    <p>Lets take a look at how the rankings change over a quarter.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Select the quarterly option from the first dropdown menu</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/awareness/ranking?Average=Quarterly",
                target: '.navSearch>.rbt',
                title: 'Product Consideration',
                content: <div className="productTourStep">
                    <p>You have seen how much of the market you have reached, and where you are relative to your core competitors.</p>
                    <p>Lets now take a look at how much of the market would consider using your product.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "consider" into the search bar</p >
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on 'Product Consideration' in the Consideration & Preference section of the list</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/consideration-and-preference/product-consideration/over-time?Average=Quarterly",
                target: '#viewSelectorToggle',
                title: 'Ranking table',
                content: <div className="productTourStep">
                    <p>Now you can see how consideration changes over time.</p>
                    <p>This is a stable metric so let's look at how we compare to a larger group of competitors on the ranking table.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Go to the "ranking table" view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/consideration-and-preference/product-consideration/ranking?Average=Quarterly&Competitors=All",
                target: '#viewSelectorToggle',
                title: 'Considerers',
                content: <div className="productTourStep">
                    <p>Lets focus on the profile of the people considering using us.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">people</i> Select the profile view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/consideration-and-preference/product-consideration/profile?Average=Quarterly&Competitors=All",
                target: '.saveChart',
                title: 'Profiles',
                content: <div className="productTourStep">
                    <p>You can see how consideration for your product changes across different demographic segments.</p>
                    <p>This is a good starting point for building hypotheses about how best to target your campaigns to convert the highest proportion of considerers into customers.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'right'
            }
        ],
        "Tour3-FS-UK-Sub": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Find the penetration reports',
                content: <div className="productTourStep">
                    <p>You want to understand who your typical customer is, and who your competitors customer are. This is useful because you can start to validate whether you are competing with other brands for the same customers.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "penetration" into the search bar</p >
                    <p>First you need to focus on people that have purchased from your brand - these are your total active customers.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on "Brand Penetration" in the list to go to the relevant charts</p>
                </div >,
                placement: "right"
            },
            {
                url: "/demand-and-usage/brand-penetration/over-time",
                target: '#viewSelectorToggle',
                title: 'Profile of your customer',
                content: <div className="productTourStep">
                    <p>You want to understand more about who your customer is.</p>
                    <p>Let's focus on the profile of the people that have used you in the last year.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">people</i> Select the profile view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/brand-penetration/profile",
                target: '.averageSelector',
                title: 'Selecting a time period',
                content: <div className="productTourStep">
                    <p>The current time period is a little short for looking at a profile.</p>
                    <p>Let’s take a look at a larger period and focus more on seasonal differences.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Select the quarterly option from the first dropdown menu</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/brand-penetration/profile?Average=Quarterly",
                target: '.periodsSelector',
                title: 'Competitors customers',
                content: <div className="productTourStep">
                    <p>You can now see how you have performed this quarter versus the last quarter.</p>
                    <p>You can now see whether or not your customer profile changes period to period, and start to think whether the strategy will need to differ throughout the year.</p>
                    <p>Now you have an understanding of who your customers are and whether this changes, let’s take a look at your competitors customer and how these differ.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Change “Current and previous period” to “Current period”</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/brand-penetration/profile?Average=Quarterly&Period=Current",
                target: '.periodsSelector',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>You can see who your competitors main customers are and whether or not you are competing for customers from the same pool.</p>
                    <p>This gives us a general understanding of who their proposition appeals to.</p>
                    <p>This has given us an overview in to your customers, and the customers your competitors. You can do more analysis on these customer bases by using the dashboard to further investigate.</p>
                </div>,
                placement: 'right'
            }
        ],

        // RETAIL TOURS:

        "Tour1-RT-UK": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Find the net buzz reports',
                content: <div className="productTourStep">
                    <p>Lets start by looking into the buzz around a brand.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "buzz" into the search bar</p >
                    <p>You can see a number of buzz questions in the list, but you are interested in "Net Buzz".</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on "Net Buzz" in the list to go to the relevant charts</p>
                </div >,
                placement: "right"
            },
            {
                url: "/brand-attention/net-buzz/over-time",
                target: '.averageSelector',
                title: 'The over time view',
                content: <div className="productTourStep">
                    <p>Net buzz is influenced by current affairs and is predictive of the future performance of a brand.</p>
                    <p>In this first view (the over time view) you can see how net buzz tracks over time compared to your key competitors.</p>
                    <p>Let&apos;s focus on this quarter and the previous quarter.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Set your view to &apos;Quarterly&apos; using the average dropdown</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-attention/net-buzz/over-time?Average=Quarterly",
                target: '#viewSelectorToggle',
                title: 'Changing chart type',
                content: <div className="productTourStep">
                    <p>Now you can see there are fewer data points and the data has become more stable. The data has been aggregated up to an average of the quarters.</p>
                    <p>You want to focus on this quarter vs the last, so lets cut out the noise.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">bar_chart</i>Select the competition view.</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-attention/net-buzz/competition?Average=Quarterly&Competitors=All",
                target: '.filterPopup',
                title: 'Filtering the results',
                content: <div className="productTourStep">
                    <p>Let&apos;s take a look at the London market.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">tune</i> Go to filters, check the London Region and then hit the Apply filters button</p>
                </div>,
                placement: 'bottom'
            },
            {
                url: "/brand-attention/net-buzz/competition?Average=Quarterly&Competitors=All&Region=L",
                target: '.saveChart',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>The chart is now ready to export and share around.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'bottom'
            }
        ],
        "Tour2-RT-UK": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Investigating conversion',
                content: <div className="productTourStep">
                    <p>Before you can look at how many people are becoming users, you need to know how much of the market you are reaching.</p>
                    <p>Let&apos;s look at the awareness metric.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "awareness" into the search bar</p >
                    <p>You can see a number of awareness questions in the list, but you are interested in <b>"Brand Health &gt; Awareness".</b></p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on 'Awareness' in the Brand Health section of the list</p>
                </div >,
                placement: "right"
            },
            {
                url: "/brand-health/awareness/over-time",
                target: '#viewSelectorToggle',
                title: 'The over time view',
                content: <div className="productTourStep">
                    <p>Brand awareness is a stable metric - especially for established brands.</p>
                    <p>This is illustrated in the ranking view.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">format_list_numbered</i> Go to the ranking view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/awareness/ranking",
                target: '.averageSelector',
                title: 'Changing period',
                content: <div className="productTourStep">
                    <p>Here you can see all of the brands next to one another and their position within the core competitor set.</p>
                    <p>This metric is normally stable and therefore should be viewed over longer periods of time.</p>
                    <p>Lets take a look at how the rankings change over a quarter.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Select the quarterly option from the first dropdown menu</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/awareness/ranking?Average=Quarterly",
                target: '.navSearch>.rbt',
                title: 'Brand Consideration',
                content: <div className="productTourStep">
                    <p>You have seen how much of the market you have reached, and where you are relative to your core competitors.</p>
                    <p>Lets now take a look at how much of the market would consider using your brand.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "consider" into the search bar</p >
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on 'Brand Consideration' in the Demand and Usage section of the list</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/consideration/ranking?Average=Quarterly&Competitors=All",
                target: '#viewSelectorToggle',
                title: 'Considerers',
                content: <div className="productTourStep">
                    <p>Lets focus on the profile of the people considering using us.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">people</i> Select the profile view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/consideration/profile?Average=Quarterly&Competitors=All",
                target: '.saveChart',
                title: 'Profiles',
                content: <div className="productTourStep">
                    <p>You can see how consideration for your brand changes across different demographic segments.</p>
                    <p>This is a good starting point for building hypotheses about how best to target your campaigns to convert the highest proportion of considerers into customers.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'right'
            }
        ],
        "Tour3-RT-UK": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Find the penetration reports',
                content: <div className="productTourStep">
                    <p>You want to understand who your typical customer is, and who your competitors customer are. This is useful because you can start to validate whether you are competing with other brands for the same customers.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "penetration" into the search bar</p >
                    <p>First you need to focus on people that have purchased from you in the last 12 months - these are your active customers.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on "penetration (last 12 months)" in the list to go to the relevant charts</p>
                </div >,
                placement: "right"
            },
            {
                url: "/demand-and-usage/penetration-last-12-months/over-time",
                target: '#viewSelectorToggle',
                title: 'Profile of your customer',
                content: <div className="productTourStep">
                    <p>You want to understand more about who your customer is.</p>
                    <p>Let's focus on the profile of the people that have used you in the last year.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">people</i> Select the profile view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/penetration-last-12-months/profile",
                target: '.averageSelector',
                title: 'Selecting a time period',
                content: <div className="productTourStep">
                    <p>The current time period is a little short for looking at a profile.</p>
                    <p>Let’s take a look at a larger period and focus more on seasonal differences.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Select the quarterly option from the first dropdown menu</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/penetration-last-12-months/profile?Average=Quarterly",
                target: '.periodsSelector',
                title: 'Competitors customers',
                content: <div className="productTourStep">
                    <p>You can now see how you have performed this quarter versus the last quarter.</p>
                    <p>You can now see whether or not your customer profile changes period to period, and start to think whether the strategy will need to differ throughout the year.</p>
                    <p>Now you have an understanding of who your customers are and whether this changes, let’s take a look at your competitors customer and how these differ.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Change “Current and previous period” to “Current period”</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/penetration-last-12-months/profile?Average=Quarterly&Period=Current",
                target: '.periodsSelector',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>You can see who your competitors main customers are and whether or not you are competing for customers from the same pool.</p>
                    <p>This gives us a general understanding of who their proposition appeals to.</p>
                    <p>This has given us an overview in to your customers, and the customers your competitors. You can do more analysis on these customer bases by using the dashboard to further investigate.</p>
                </div>,
                placement: 'right'
            }
        ],

        // WGSN BAROMETER TOURS:

        "Tour1-WGSN-UK": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Find the net buzz reports',
                content: <div className="productTourStep">
                    <p>Lets start by looking into the buzz around a brand.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "buzz" into the search bar</p >
                    <p>You can see a number of buzz questions in the list, but you are interested in "Net Buzz".</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on "Net Buzz" in the list to go to the relevant charts</p>
                </div >,
                placement: "bottom"
            },
            {
                url: "/media-performance/net-buzz/over-time",
                target: '.averageSelector',
                title: 'The over time view',
                content: <div className="productTourStep">
                    <p>Net buzz is influenced by current affairs and is predictive of the future performance of a brand.</p>
                    <p>In this first view (the over time view) you can see how net buzz tracks over time compared to your key competitors.</p>
                    <p>Let&apos;s focus on this quarter and the previous quarter.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Set your view to &apos;Quarterly&apos; using the average dropdown</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/media-performance/net-buzz/over-time?Average=Quarterly",
                target: '#viewSelectorToggle',
                title: 'Changing chart type',
                content: <div className="productTourStep">
                    <p>Now you can see there are fewer data points and the data has become more stable. The data has been aggregated up to an average of the quarters.</p>
                    <p>You want to focus on this quarter vs the last, so lets cut out the noise.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">bar_chart</i>Select the competition view.</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/media-performance/net-buzz/competition?Average=Quarterly&Competitors=All",
                target: '.filterPopup',
                title: 'Filtering the results',
                content: <div className="productTourStep">
                    <p>Let&apos;s take a look at the London market.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">tune</i> Go to filters, check the London Region and then hit the Apply filters button</p>
                </div>,
                placement: 'bottom'
            },
            {
                url: "/media-performance/net-buzz/competition?Average=Quarterly&Competitors=All&Region=L",
                target: '.saveChart',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>The chart is now ready to export and share around.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'bottom'
            }
        ],
        "Tour2-WGSN-UK": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Investigating conversion',
                content: <div className="productTourStep">
                    <p>Before you can look at how many people are becoming users, you need to know how much of the market you are reaching.</p>
                    <p>Let&apos;s look at the awareness metric.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "awareness" into the search bar</p >
                    <p>You can see a number of awareness questions in the list, but you are interested in <b>"Brand Health &gt; Prompted Awareness".</b></p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on 'Prompted Awareness' in the Brand Health section of the list</p>
                </div >,
                placement: "bottom"
            },
            {
                url: "/brand-health/prompted-awareness/over-time",
                target: '#viewSelectorToggle',
                title: 'The over time view',
                content: <div className="productTourStep">
                    <p>Brand awareness is a stable metric - especially for established brands.</p>
                    <p>This is illustrated in the ranking view.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">format_list_numbered</i> Go to the ranking view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/prompted-awareness/ranking",
                target: '.averageSelector',
                title: 'Changing period',
                content: <div className="productTourStep">
                    <p>Here you can see all of the brands next to one another and their position within the core competitor set.</p>
                    <p>This metric is normally stable and therefore should be viewed over longer periods of time.</p>
                    <p>Lets take a look at how the rankings change over a quarter.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Select the quarterly option from the first dropdown menu</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/prompted-awareness/ranking?Average=Quarterly",
                target: '.navSearch>.rbt',
                title: 'Consideration',
                content: <div className="productTourStep">
                    <p>You have seen how much of the market you have reached, and where you are relative to your core competitors.</p>
                    <p>Lets now take a look at how much of the market would consider using your brand.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "consider" into the search bar</p >
                    <p>You are interested in <b>"Brand Health &gt; Consideration".</b></p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on 'Consideration' in the Brand Health section of the list</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/consideration/ranking?Average=Quarterly&Competitors=All",
                target: '#viewSelectorToggle',
                title: 'Considerers',
                content: <div className="productTourStep">
                    <p>Lets focus on the profile of the people considering using us.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">people</i> Select the profile view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/brand-health/consideration/profile?Average=Quarterly&Competitors=All",
                target: '.saveChart',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>You can see how consideration for your brand changes across different demographic segments.</p>
                    <p>This is a good starting point for building hypotheses about how best to target your campaigns to convert the highest proportion of considerers into customers.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'bottom'
            }
        ],
        "Tour3-WGSN-UK": [
            {
                url: "/getting-started",
                target: '.navSearch>.rbt',
                title: 'Find the penetration reports',
                content: <div className="productTourStep">
                    <p>You want to understand who your typical customer is, and who your competitors customer are. This is useful because you can start to validate whether you are competing with other brands for the same customers.</p>
                    <p className="action mb-3"><i className="material-symbols-outlined  float-start me-2">keyboard</i> Type "penetration" into the search bar</p >
                    <p>First you need to focus on people that have purchased from you in the last 12 months (L12M) - these are your active customers.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Click on "Penetration (L12M)" in the list to go to the relevant charts</p>
                </div >,
                placement: "bottom"
            },
            {
                url: "/demand-and-usage/penetration-l12m/over-time",
                target: '#viewSelectorToggle',
                title: 'Profile of your customer',
                content: <div className="productTourStep">
                    <p>You want to understand more about who your customer is.</p>
                    <p>Let's focus on the profile of the people that have used you in the last year.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">people</i> Select the profile view</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/penetration-l12m/profile",
                target: '.averageSelector',
                title: 'Selecting a time period',
                content: <div className="productTourStep">
                    <p>The current time period is a little short for looking at a profile.</p>
                    <p>Let’s take a look at a larger period and focus more on seasonal differences.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Select the quarterly option from the first dropdown menu</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/penetration-l12m/profile?Average=Quarterly",
                target: '.periodsSelector',
                title: 'Competitors customers',
                content: <div className="productTourStep">
                    <p>You can now see how you have performed this quarter versus the last quarter.</p>
                    <p>You can now see whether or not your customer profile changes period to period, and start to think whether the strategy will need to differ throughout the year.</p>
                    <p>Now you have an understanding of who your customers are and whether this changes, let’s take a look at your competitors customer and how these differ.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">touch_app</i> Change “Current and previous period” to “Current period”</p>
                </div>,
                placement: 'right'
            },
            {
                url: "/demand-and-usage/penetration-l12m/profile?Average=Quarterly&Period=Current",
                target: '.saveChart',
                title: 'Export the chart',
                content: <div className="productTourStep">
                    <p>You can see who your competitors main customers are and whether or not you are competing for customers from the same pool.</p>
                    <p>This gives us a general understanding of who their proposition appeals to.</p>
                    <p>This has given us an overview in to your customers, and the customers your competitors. You can do more analysis on these customer bases by using the dashboard to further investigate.</p>
                    <p className="action"><i className="material-symbols-outlined  float-start me-2">get_app</i> Click the "Save chart" button and the graph will be downloaded</p>
                </div>,
                placement: 'bottom'
            }
        ],

    };
}
