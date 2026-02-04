import { createTheme } from "@mui/material";

declare module '@mui/material/styles' {
    interface TypographyVariants {
        info: React.CSSProperties;
    }

    // allow configuration using `createTheme()`
    interface TypographyVariantsOptions {
        info?: React.CSSProperties;
    }
}

// Update the Typography's variant prop options
declare module '@mui/material/Typography' {
    interface TypographyPropsVariantOverrides {
        info: true;
    }
}

const theme = createTheme({
    palette: {
        background: {
            paper: '#ffffff',
            default: '#f8f9f8'
        },
        secondary: {
            main: '#1376CD1A', // Define the main secondary color
            contrastText: '#000000' // Define the contrast text color for secondary
        }
    },
    typography: {
        h1: {
            fontSize: '2rem', // Adjust the size as needed
        },
        h2: {
            fontSize: '1.75rem', // Adjust the size as needed
        },
        h3: {
            fontSize: '1.5rem', // Adjust the size as needed
        },
        h4: {
            fontSize: '1.25rem', // Adjust the size as needed
        },
        h5: {
            fontSize: '1rem', // Adjust the size as needed
        },
        h6: {
            fontSize: '0.875rem', // Adjust the size as needed
        },
        info: {
            color: '#717886'
        },
    },
    components: {
        MuiDialog: {
            styleOverrides: {
                root: {
                    "& .MuiDialog-container": {
                        "& .MuiPaper-root": {
                            width: "100%",
                            maxWidth: "800px",
                        },
                    },
                }
            }
        },
        MuiDialogTitle: {
            styleOverrides: {
                root: {
                    textAlign: 'center',
                    fontSize: 'large',
                    padding: '30px 50px',
                }
            }
        },
        MuiDialogContent: {
            styleOverrides: {
                root: {
                    padding: '30px 50px',
                }
            }
        },
        MuiDialogActions: {
            styleOverrides: {
                root: {
                    justifyContent: 'center',
                    gap: '5px',
                    padding: '30px 50px',
                }
            }
        },
        MuiButton: {
            styleOverrides: {
                root: {
                    textTransform: 'none', // Stop default capitalization
                },
            },
        },
        MuiTab: {
            styleOverrides: {
                root: {
                    textTransform: 'none', // Stop default capitalization for tab headers
                },
            },
        },
        MuiMenuItem: {
            styleOverrides: {
                root: {
                    fontSize: '0.875rem',
                    '&:hover, &:focus-visible': {
                        backgroundColor: '#E2F1FD',
                    },
                },
            },
        },
        MuiChip: {
            styleOverrides: {
                root: {
                    fontSize: '0.875rem',
                    borderRadius: "4px",
                    lineHeight: '1.25rem',
                    '&.small': {
                        fontSize: '0.75rem',
                        lineHeight: '1.125rem'
                    }
                },
            },
        },
        MuiTypography: {
            styleOverrides: {
                root: {
                    '&.quote': {
                        color: "#000000BF",
                        '&::before': {
                            content: '"\\201C"'
                        },
                        '&::after': {
                            content: '"\\201D"'
                        },
                    },
                    '&.actionText': {
                        color: "#1376CD",
                        fontWeight: 400
                    }
                }
            }
        },
        MuiTooltip: {
            styleOverrides: {
                tooltip: {
                    fontSize: '0.875rem',
                    color: '#fff',
                    backgroundColor: '#000'
                }
            }
        },
        MuiListSubheader: {
            styleOverrides: {
                root: {
                    fontSize: '0.75rem',
                    lineHeight: '1rem',
                    marginTop: '10px',
                    paddingLeft: '10px'
                }
            }
        },
        MuiAutocomplete: {
            styleOverrides: {
                root: {
                    '& .MuiInputBase-root': {
                        height: '40px'
                    },
                    '& .MuiAutocomplete-inputRoot': {
                        paddingTop: '0px',
                        paddingBottom: '0px'
                    },
                    '& .MuiAutocomplete-listbox': {
                        maxHeight: '400px'
                    },
                    '& .MuiPaper-root': {
                        marginTop: '4px' // Space between input and dropdown
                    }
                }
            }
        },
        MuiTextField: {
            styleOverrides: {
                root: {
                    '& .MuiFormHelperText-root': {
                        width: '100%',
                        margin: 0,
                        padding: '2px 10px'
                    }
                }
            }
        }
    },
});

export default theme;