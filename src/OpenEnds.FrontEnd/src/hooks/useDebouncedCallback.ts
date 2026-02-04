import { useCallback, useEffect, useRef } from "react";

function useDebouncedCallback<T extends (...args: any[]) => void>(callback: T, delay: number) {
    const handlerRef = useRef<number | undefined>();
  
    const debouncedFunction = useCallback((...args: Parameters<T>) => {
      // Clear the existing timeout
      if (handlerRef.current !== undefined) {
        window.clearTimeout(handlerRef.current);
      }
  
      // Set a new timeout
      handlerRef.current = window.setTimeout(() => {
        callback(...args);
      }, delay);
    }, [callback, delay]);
  
    // Cancel the timeout if the component is unmounted or delay/callback changes
    useEffect(() => {
      return () => {
        if (handlerRef.current !== undefined) {
          window.clearTimeout(handlerRef.current);
        }
      };
    }, []);
  
    return debouncedFunction;
  }

export default useDebouncedCallback;