import React from "react";

export function useLocalStorage(key: string, initialValue: any) {
    const [storedValue, setStoredValue] = React.useState(() => {
      if (typeof window === "undefined") {
        return initialValue;
      }
      try {
        const item = window.localStorage.getItem(key);
        if(item) {
            return item ? JSON.parse(item) : initialValue;
        }
      } catch (error) {
        console.log(error);
      }
    });

    const setValue = (value: string) => {
      try {
        setStoredValue(value);
        if (typeof window !== "undefined") {
          window.localStorage.setItem(key, JSON.stringify(value));
        }
      } catch (error) {
        console.log(error);
      }
    };
    return [storedValue, setValue];
  }