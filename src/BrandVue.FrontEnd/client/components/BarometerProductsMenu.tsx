import React from "react"
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';

const BarometerProductsMenu = () => {
    const [isOpen, setIsOpen] = React.useState(false);

    const navigate = (url) => {
        window.location = url;
    }

    return (
            <ButtonDropdown isOpen={isOpen} toggle={() => setIsOpen(!isOpen)} className="products item menu">
                    <DropdownToggle className="nav-button" tag="div">    
                        <div className="product-dropdown-toggle">
                             <span>Barometer</span>
                             <i className="material-symbols-outlined">arrow_drop_down</i>
                        </div>
                    </DropdownToggle>
                    <DropdownMenu className="barometer-products">
                        <DropdownItem className="product-bar-link insight" tag="div" key="insight" onClick={() => navigate("https://www.wgsn.com/insight")}><a href="https://www.wgsn.com/insight" className="product-bar-link insight">Insight</a></DropdownItem>
                        <DropdownItem className="product-bar-link wgsn" tag="div" key="fashion" onClick={() => navigate("https://www.wgsn.com/fashion")}><a href="https://www.wgsn.com/fashion" className="product-bar-link wgsn">Fashion</a></DropdownItem>
                        <DropdownItem className="product-bar-link in-stock" tag="div" key="beauty" onClick={() => navigate("https://www.wgsn.com/beauty")}><a href="https://www.wgsn.com/beauty" className="product-bar-link in-stock">Beauty</a></DropdownItem>
                        <DropdownItem className="product-bar-link mindset" tag="div" key="foodanddrink" onClick={() => navigate("https://www.wgsn.com/fd")}><a href="https://www.wgsn.com/fd" className="product-bar-link mindset">Food &amp; Drink</a></DropdownItem>
                        <DropdownItem className="product-bar-link homebuildlife" tag="div" key="lifestyleandinteriors" onClick={() => navigate("https://www.wgsn.com/li?lang=en")}><a href="https://www.wgsn.com/li?lang=en" className="product-bar-link homebuildlife">Lifestyle &amp; Interiors</a></DropdownItem>
                        <DropdownItem className="product-bar-link futures" tag="div" key="citybycity" onClick={() => navigate("https://www.wgsn.com/citybycity")}><a href="https://www.wgsn.com/citybycity" className="product-bar-link futures">City by City</a></DropdownItem>
                        <DropdownItem className="product-bar-link barometer" tag="div" key="barometer" onClick={() => navigate("https://wgsn.com/barometer/introducing")}><a href="https://wgsn.com/barometer/introducing" className="product-bar-link barometer active">Barometer</a></DropdownItem>
                        <DropdownItem className="product-bar-link styletrial" tag="div" key="advisory" onClick={() => navigate("https://www.wgsn.com/content/introducing/advisory")}><a href="https://www.wgsn.com/content/introducing/advisory" className="product-bar-link styletrial">Advisory</a></DropdownItem>
                    </DropdownMenu>
            </ButtonDropdown>
    );
}

export default BarometerProductsMenu;