function replace_base_url() {
    if (!location.href.startsWith("file")) {
        const developersIndex = location.href.indexOf('/developers');
        let domainName = location.href.substring(0, developersIndex);
        if (location.origin.indexOf("developers.savanta.com") > -1) {
            domainName = "https://{YourOrganizationCode}.all-vue.com/{Product}";
        }
        if (domainName !== "") {
            let subProduct = null;
            try {
                const urlParams = new URLSearchParams(window.location.search);
                subProduct = urlParams.get('subProduct');
            } catch(err) {
                // Likely an old browser
                console.error("Making best effort at root uri due to :\r\n" + err);
            }
            if (subProduct) {
                domainName += "/" + subProduct;
            }
            
            replace_variables('https://{DomainAndOrg}/{ProductName}', domainName);
            $('a#base-url-followed-by-variables').parent().next('ul').hide();
        }
    }
}

function replace_variables(toFind, replaceWith) {
	//https://stackoverflow.com/a/17820079
	toFind = toFind.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&');
    const regex = new RegExp(toFind, 'g');

    const replaceFn = function($textNode, matchedText, match) {
        let parentNode = $textNode.parent();
        if (parentNode.is("a")) {
            parentNode.attr("href", replaceWith);
        }

        $textNode.replaceWith(replaceWith);
    }

    $('body').safeReplace(regex, replaceFn);
}
