import React from "react";

/*
 reactstrap's DropdownToggle component defines an `aria-haspopup` prop that conflicts with the attribute defined for HTMLElement.
 So, annoyingly, we need to define our own interface that does the same thing 🙃
*/
export interface IDropdownToggleAttributes extends React.HTMLAttributes<HTMLElement> {
    'aria-haspopup'?: boolean;
}
