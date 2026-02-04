import React from "react";
import { ChangeEvent, useEffect, useState } from "react";
import * as BrandVueApi from "../BrandVueApi";
import { IMetaDataClient, INamedInstanceColourModel } from "../BrandVueApi";
import Throbber from "../components/throbber/Throbber";
import _ from "lodash";
import Footer from "../components/Footer";
import SearchInput from "../components/SearchInput";

interface IPageProps {
    nav: React.ReactNode;
}

export const ColourConfigurationPage: React.FunctionComponent<IPageProps> = ((props: IPageProps) => {

    const metadataClient: IMetaDataClient = BrandVueApi.Factory.MetaDataClient(() => {});
    const [colourConfigurations, setColourConfigurations] = useState<INamedInstanceColourModel[]>([]);
    const [searchString, setSearchString] = useState("");
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [error, setError] = useState<Error|undefined>(undefined);

    useEffect(() => {
        metadataClient.getEntityInstanceColours("brand")
            .then(values => setColourConfigurations(_.orderBy(values, v => v.instanceName)))
            .then(() => setIsLoading(false))
            .catch((e: Error) => {
                setIsLoading(false);
                setError(e);
            });
    }, [])

    const updateColour: (instanceId: number, colour: string) => Promise<void> = async (instanceId, colour) => {
        const copy = [...colourConfigurations];
        const toUpdate = copy.find(c => c.instanceId === instanceId)!;
        toUpdate.colour = colour;
        setColourConfigurations(copy);

        if (colour === "") {
            await metadataClient.removeEntityInstanceColour("brand", instanceId);
        } else {
            await metadataClient.saveEntityInstanceColour("brand", instanceId, colour);
        }
    };

    if (isLoading) return (
        <div id="ld" className="loading-container">
            <Throbber />
        </div>
    );

    if (error) {
        return (
            <div id="colour-configuration-page">
                {props.nav}
                <div className="error-container">
                    <p>{error.message}</p>
                </div>
                <Footer />
            </div>
        );
    }

    const filteredConfigurations = searchString.trim().length > 0 ?
        colourConfigurations.filter(c => c.instanceName.toLowerCase().includes(searchString.trim().toLowerCase()))
        : colourConfigurations;

    const productIdentifier = `${window.location.host}/${(window as any).productName}`.replace("//", "/");

    return (
        <div id="colour-configuration-page">
            {props.nav}
            <div className="scroll-area">
                <div className="scroll-area-center">
                    <div className="top-section">
                        <h1>Configure brand colours</h1>
                        <p>By default, charts use random preset colours. To use a custom colour for a brand, specify a hex colour code.</p>
                        <p>These colours will be set for all users of <strong>{productIdentifier}</strong></p>
                        <SearchInput id="colour-search" text={searchString} onChange={(text) => setSearchString(text)} className="colour-search" />
                    </div>
                    <div className="configuration-list">
                        {filteredConfigurations.map(c => <ColourConfiguration key={c.instanceId} {...c} updateColour={updateColour} />)}
                    </div>
                </div>
            </div>
        </div>
    );
});

interface IColourConfigurationProps {
    instanceId: number;
    instanceName: string;
    colour: string | undefined;
    updateColour: (instanceId: number, colour: string) => Promise<void>;
}

const ColourConfiguration: React.FunctionComponent<IColourConfigurationProps> = ((props: IColourConfigurationProps) => {

    const initialValue = hashless(props.colour?.toUpperCase() ?? "");
    const [inputValue, setInputValue] = useState(initialValue);

    const saveColour = async (newColour: string): Promise<boolean> => {
        const bestChance = hashless(newColour).trim().toUpperCase();
        const colourChanged = bestChance != initialValue;
        if (newColour === "" && colourChanged) {
            await props.updateColour(props.instanceId, "");
            return true;
        }
        if (hexColourRegex.test(bestChance) && colourChanged) {
            await props.updateColour(props.instanceId, addHash(bestChance));
            return true;
        }
        return false;
    }

    const handleColourChange = async (event: ChangeEvent<HTMLInputElement>): Promise<void> => {
        const newColour = hashless(event.target.value);
        setInputValue(newColour);
        await saveColour(newColour);
    }

    const handleBlur = async (event: ChangeEvent<HTMLInputElement>): Promise<void> => {
        const colourSaved = await saveColour(event.target.value);
        if (!colourSaved) {
            setInputValue(initialValue);
        }
    }

    const inputName = `input-${props.instanceId}`;
    return (
        <div className="colour-configuration">
            <label htmlFor={inputName} className="brand-name">{props.instanceName}</label>
            <div className="colour-input-group">
                <div className="input-unit">#</div>
                <input id={inputName} className="colour-input" type="text" spellCheck={false} autoComplete="off" autoCapitalize="characters" value={inputValue} onKeyPress={interceptHash} onChange={handleColourChange} onBlur={handleBlur} />
                <div className="colour-preview">
                    {initialValue && <div className="colour" style={{backgroundColor: addHash(initialValue)}}></div>}
                </div>
            </div>
        </div>
    )
});

const hexColourRegex = /^[0-9A-F]{6}$/i;

const hashless = (s: string): string => {
    return s.replace("#", "");
}

const addHash = (s: string): string => {
    return "#" + s;
}

const interceptHash = (event: React.KeyboardEvent<HTMLInputElement>): void => {
    if (event.key === "#") {
        event.currentTarget.setSelectionRange(0, event.currentTarget.value.length);
        event.preventDefault();
    }
}