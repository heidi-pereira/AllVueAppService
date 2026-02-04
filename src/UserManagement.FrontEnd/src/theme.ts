import { createTheme, ThemeOptions, PaletteColorOptions } from '@mui/material/styles';
import type { } from '@mui/x-data-grid/themeAugmentation';
import { grey } from '@mui/material/colors';

declare module '@mui/material/Typography' {
  interface TypographyPropsVariantOverrides {
    formlabel: true;
    questionListTitle: true;
    questionListDescription: true;
    questionListPercent: true;
    userListName: true;
    userListEmail: true;
    filterListOption: true;
    questionModalName: true;
    questionModalDescription: true;
    dataGroupCurrentlyIn: true;
    responsesPercentageBar: true;
    backButton: true;
    dropDownTitle: true,
    alertHeading: true;
    alertBody: true;
  }
}

declare module '@mui/material/Button' {
  interface ButtonPropsColorOverrides {
    cancel: true;
  }
}

declare module '@mui/material/styles' {
    interface PaletteOptions {
        cancel?: PaletteColorOptions;
        slate?: PaletteColorOptions;
    }
}

const brandTheme : ThemeOptions = {
    palette: {
        primary: {
            main: '#1376CD',
            dark: '#0e4b81',
        },
        info: {
            main: '#0235B9',
            light: '#DDEEFF',
            dark: '#021A66',
        },
        cancel: {
            // https://mui.com/material-ui/customization/palette/#custom-colors
            main: '#EEEFF0',
            light: '#EEEFF0',
            dark: '#26292C33',
            contrastText: '#42484D',
        },
        slate: {
            main: '#6E7881',
            light: '#A0A7AE',
            dark: '#26292C',
        }
    },
};

export const InfoThemes = createTheme({
    ...brandTheme,
    palette: {
        ...brandTheme.palette,
    },
    components: {
        MuiInputBase: {
            styleOverrides: {
                root: {
                    '&.Mui-disabled': {
                        backgroundColor: '#f5f5f5'
                    },
                },
            },
        },
        MuiDialogTitle: {
            styleOverrides: {
                root: {
                    marginTop: '20px',
                    padding: '20px 24px 0 24px',
                    fontWeight: 400,
                    height: '60px',

                    fontSize: '1.25rem',
                    lineHeight: 1.5,
                    '& .MuiIconButton-root': {
                        position: 'absolute',
                        right: '40px',
                        top: '15px',
                    },
                },
            },
        },
        MuiDialogActions: {
            styleOverrides: {
                root: {
                    justifyContent: 'center',
                    paddingBottom: '20px',
                },
            },
        },
        MuiDataGrid: {
            styleOverrides: {
                root: {
                    border: 'none',
                    '& .MuiDataGrid-scrollbar': {
                        scrollbarColor: '#acacac #e5e5e5',
                        scrollbarWidth: 'thin',
                    },
                    '& .MuiDataGrid-scrollbar::-webkit-scrollbar': {
                        width: '6px',
                        height: '6px',
                    },
                    '& .MuiDataGrid-scrollbar::-webkit-scrollbar-thumb': {
                        backgroundColor: '#acacac',
                        borderRadius: '4px',
                    },
                    '& .MuiDataGrid-scrollbar::-webkit-scrollbar-track': {
                        backgroundColor: '#e5e5e5',
                    },
                    '& .MuiDataGrid-cell': {
                        border: 'none',
                        '& a': {
                            color: '#1976d2',
                            textDecoration: 'none',
                        },
                    },
                    '& .MuiDataGrid-row': {
                    },
                    '& .MuiDataGrid-columnHeaderTitle': {
                    },
                    '& .MuiDataGrid-selectedRowCount': {
                    },
                    '& .MuiCheckbox-root': {
                    },
                    '& .MuiDataGrid-columnHeaders .MuiDataGrid-scrollbarFiller': {
                        backgroundColor: '#F0F0F0',
                        border: 'none',
                    },
                    '& .MuiLinearProgress-root': {
                        height: '4px',
                    },
                    '& .MuiDataGrid-virtualScroller': { border: '2px solid #F0F0F0' },

                },
                columnHeader: {
                    backgroundColor: '#F0F0F0',
                    '& .MuiSvgIcon-root': {
                    },

                },
                columnHeaderTitle: {
                },
                columnHeaderSortIcon: {
                },
                footerContainer: {
                },
                scrollbar: {
                    scrollbarColor: '#acacac #e5e5e5',
                    scrollbarWidth: 'thin',
                }
            },
        },
        MuiFormControl: {
            styleOverrides: {
                root: {
                    '& .MuiSvgIcon-root': {
                    }
                }
            }
        },
        MuiTablePagination: {
            styleOverrides: {
                toolbar: {
                    '& .MuiButtonBase-root': {
                    }
                },
                selectIcon: {
                },
                root: {
                    '& .MuiSvgIcon-colorPrimary': {
                    },

                }
            }
        },
        MuiSelect: {
            styleOverrides: {
                root: {
                    backgroundColor: '#fff',
                    border: '1px solid #cbcfd3',
                    borderRadius: '4px',
                    height: '40px',
                    '& .MuiSelect-select': {
                        padding: '7px',
                        fontFamily: 'inherit',
                        fontSize: 'inherit',
                        lineHeight: 'inherit',
                    },
                    '& .MuiOutlinedInput-notchedOutline': {
                        border: 'none',
                    },
                    '&:hover': {
                        borderColor: '#cbd5e1',
                    },
                    '&.Mui-focused': {
                        borderColor: '#4682b4',
                        boxShadow: '0px 0px 2px 2px rgba(182,216,247,0.56)',
                    },
                    '&.Mui-disabled': {
                        backgroundColor: '#f5f5f5'
                    },
                },
            },
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
        MuiIconButton: {
            styleOverrides: {
                root: {
                    '& .MuiSvgIcon-root': {
                    }
                },
            }
        },
        MuiTypography: {
            defaultProps: {
                variantMapping: {
                    formlabel: 'p',
                    questionListTitle: 'p',
                    questionListDescription: 'p',
                    questionListPercent: 'span',
                    userListName: 'span',
                    userListEmail: 'span',
                    dataGroupCurrentlyIn: 'span',
                    filterListOption: 'div',
                    questionModalName: 'span',
                    questionModalDescription: 'div',
                    responsesPercentageBar: 'span',
                    backButton: 'span',
                    dropDownTitle: 'span',
                    alertHeading: 'div',
                    alertBody: 'div',
                }
            },
            styleOverrides: {
                root: ({ theme }) => ({
                    '&.MuiFormControlLabel-label': {
                        fontSize: 14,
                    },
                    variants: [
                        {
                            props: { variant: 'formlabel'},
                            style: {
                                fontWeight: 500,
                                marginBottom: '0.25rem',
                            }
                        },
                        {
                            props: { variant: 'questionListTitle'},
                            style: {
                                fontWeight: 400,
                                fontSize: 11,
                                color: theme.palette.slate.main,
                                overflow: 'hidden',
                                textOverflow: 'ellipsis',
                                whiteSpace: 'nowrap'
                            }
                        },
                        {
                            props: { variant: 'questionListDescription'},
                            style: {
                                display: '-webkit-box',
                                '-webkit-line-clamp': '2',
                                '-webkit-box-orient': 'vertical',
                                overflow: 'hidden',
                                fontWeight: 500,
                                mt: 0.2,
                                fontSize: 13,
                                textOverflow: 'ellipsis',
                                color: theme.palette.slate.dark,
                            }
                        },
                        {
                            props: { variant: 'questionListPercent'},
                            style: {
                                fontWeight: 400,
                                fontSize: 11,
                                minWidth: 28,
                                textAlign: 'right',
                                color: theme.palette.slate.main,
                            }
                        },
                        {
                            props: { variant: 'userListName'},
                            style: {
                                maxWidth: 420,
                                color: grey[600],
                                fontSize: 12,
                            }
                        },
                        {
                            props: { variant: 'userListEmail'},
                            style: {
                                opacity: 0.8,
                                minWidth: 36,
                                color: grey[900],
                                fontSize: 10,
                            }
                        },
                        {
                            props: { variant: 'filterListOption'},
                            style: {
                                maxWidth: 420,
                                color: grey[600],
                                overflow: 'hidden',
                                textOverflow: 'ellipsis',
                                display: '-webkit-box',
                                '-webkit-line-clamp': '2',
                                'line-clamp': '2',
                                '-webkit-box-orient': 'vertical'
                            }
                        },
                        {
                            props: { variant: 'questionModalName'},
                            style: {
                                fontWeight: 600,
                                fontSize: 12,
                                color: grey[600],
                            }
                        },
                        {
                            props: { variant: 'questionModalDescription'},
                            style: {
                                fontSize: 14,
                                lineHeight: 1.4,
                                overflow: 'hidden',
                                textOverflow: 'ellipsis',
                                textWrapMode: 'wrap',
                                display: '-webkit-box',
                                '-webkit-line-clamp': '2',
                                'line-clamp': '2',
                                '-webkit-box-orient': 'vertical'
                            }
                        },
                        {
                            props: { variant: 'responsesPercentageBar'},
                            style: {
                                fontSize: 12,
                            }
                        },
                        {
                            props: { variant: 'backButton'},
                            style: {
                                fontSize: 12,
                            }
                        },
                        { props: { variant: 'dropDownTitle'},
                            style: {
                                fontSize: 14,
                            }
                        },
                        { props: { variant: 'dataGroupCurrentlyIn'},
                            style: {
                                fontSize: 12,
                            }
                        },
                        { props: { variant: 'alertHeading'},
                            style: {
                                fontSize: 14,
                                fontWeight: 500,
                                color: theme.palette.slate.dark,
                            }
                        },
                        { props: { variant: 'alertBody'},
                            style: {
                                fontSize: 12,
                                fontWeight: 400,
                                color: theme.palette.slate.dark,
                                'a': {
                                    color: theme.palette.slate.dark,
                                    textDecoration: 'underline',
                                    cursor: 'pointer',
                                    fontWeight: 500,
                                }
                            }
                        }
                    ]
                })
            }
        },
        MuiLinearProgress: {
            styleOverrides: {
                root: {
                    height: '12px',
                    backgroundColor: '#E0E2E5',
                }
            },
        },
        MuiButton: {
            defaultProps: {
                disableRipple: true,
            },
            styleOverrides: {
                root: ({ theme }) => ({
                    textTransform: 'none',
                    boxShadow: 'none',
                    fontSize: 14,
                    lineHeight: 1,
                    letterSpacing: 0,
                    padding: '8px 12px',
                    '&:hover': {
                        boxShadow: 'none',
                    },
                    '&:focus, &:focus-visible': {
                        outline: 'none',
                        boxShadow: 'none',
                    },
                    height: 32,
                    maxHeight: 32,
                    minWidth: 80,
                    variants: [
                        {
                            props: { variant: 'contained', color: 'primary' },
                            style: {
                                '&.Mui-disabled': {
                                    backgroundColor: theme.palette.primary.main,
                                    color: theme.palette.primary.contrastText,
                                    opacity: 0.32,
                                },
                            }
                        }
                    ]
                })
            },
        },
        MuiAlert: {
            styleOverrides: {
                root: {
                    alignItems: 'center',
                    marginTop: '24px',
                    marginBottom: '16px',
                    '& .MuiTypography-body2': {
                        fontSize: 12,
                    },
                    '& .MuiTypography-body1': {
                        fontSize: 14,
                        fontWeight: 500,
                    },
                    '& .MuiAlert-icon': {
                        alignItems: 'center',
                        display: 'flex',
                        fontSize: 24,
                        marginRight: '16px',
                    }
                },
                standardInfo:  ({ theme }) => ({
                    backgroundColor: theme.palette.info.light,
                    color: '#0235B9',
                }),
            }
        },
        MuiList: {
            styleOverrides: {
                root: ({ theme }) => ({
                    scrollbarColor: `${theme.palette.slate.light} #eeeff0`,
                    scrollbarWidth: 'thin',
                    '&::-webkit-scrollbar': {
                        width: '6px',
                        height: '6px',
                    },
                    '&::-webkit-scrollbar-thumb': {
                        backgroundColor: theme.palette.slate.light,
                        borderRadius: '4px',
                    },
                    '&::-webkit-scrollbar-track': {
                        backgroundColor: '#eeeff0',
                    },
                })
            }
        },
    },
});