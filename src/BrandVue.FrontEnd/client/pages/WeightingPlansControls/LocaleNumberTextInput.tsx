import React, { useState } from 'react';

type Props = Omit<React.InputHTMLAttributes<HTMLInputElement>, 'value'> & {
  value?: number;
};

const LocaleNumberTextInput = (props: Props) => {
    const [isFocused, setIsFocused] = useState(false);

    const displayedValue = isFocused ?
        props.value?.toString() ?? '' :
        props.value?.toLocaleString() ?? '';

    return (
        <input
            {...props}
            type="text"
            value={displayedValue}
            onFocus={() => setIsFocused(true)}
            onBlur={() => setIsFocused(false)}
        />
    );
};
export default LocaleNumberTextInput;