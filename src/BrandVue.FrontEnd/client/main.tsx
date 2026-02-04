import 'whatwg-fetch';

import React from 'react';
import ReactDOM from 'react-dom/client';
import $ from 'jquery';
import 'moment/locale/en-gb';
import 'material-symbols/outlined.css';
import 'material-icons/iconfont/material-icons.css';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap';
import "../less/main.less";

import Freshchat from './freshchat';
import pageVariables from './PageVariables';
import VueApp from './VueApp';

$(() => {
    pageVariables.populate();

    // This is needed for the js injected via WGSN's google tag manager implementation
    window["jQuery"] = $;

    $.ajax({
        url: 'https://wchat.freshchat.com/js/widget.js',
        dataType: 'script',
        cache: true, // otherwise will get fresh copy every page load
        success: function () {
            Freshchat.GetOrCreateWidget().CompletedLoadingJavaScript();
        }
    });

    const root = ReactDOM.createRoot(document.getElementById('reactApp') as Element);
    root.render(<VueApp callback={ responsiveSetup } />);
});

function responsiveSetup() {
    // Utility for responsive top menu
    $('.product-dropdown-toggle').click(() => {
        $('.products .options').toggleClass('open');
    });

    $(window).resize(function () {
        if (!$('.mainmenu.menu-barometer').is(':visible')) {
            $('.sidebar.sidebar-barometer, .mainmenu.menu-barometer').removeClass('active');
        }
    });

    // Utility for responsive side menu
    $(document).on('click', '.mainmenu.menu-barometer',
        () => {
            $('.sidebar.sidebar-barometer, .mainmenu.menu-barometer').toggleClass('active');
        });

    $(document).on('click', '.sidebar.sidebar-barometer li a', () => {
        $('.sidebar.sidebar-barometer, .mainmenu.menu-barometer').removeClass('active');
    });
}