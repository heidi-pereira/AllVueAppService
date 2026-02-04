import { Significance } from "../../../../../BrandVueApi";

export const getSignificance = (significance: Significance, downIsGood: boolean) => {
    switch(significance){
        case(Significance.Up):
            return `<i class="material-symbols-outlined ${downIsGood ? "sig-red" : "sig-green"}"}>arrow_upward</i>`
        case(Significance.Down):
            return `<i class="material-symbols-outlined ${downIsGood ? "sig-green" : "sig-red"}">arrow_downward</i>`
    }
    return "";
}