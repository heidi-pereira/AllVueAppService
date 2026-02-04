// See https://github.com/gilbarbara/react-joyride/blob/3e08384415a831b20ce21c8423b6c271ad419fbf/src/styles.js for overridable styles for joyride

export default class JoyRideStyles {
    public static styles: any = {
        tooltip: {
            backgroundColor: '#CFEDFB',
            fontSize: 14,
            padding: 24,
            borderRadius: 4,
            filter: 'drop-shadow(4px 4px 12px rgba(0, 0, 0, 0.56))',
        },
        tooltipContainer: {
            textAlign: 'left',
        },
        tooltipTitle: {
            fontSize: 18,
        },
        tooltipContent: {
            padding: 'none',
        },
        buttonClose: {
            padding: 12,
        },
        buttonNext: {
            display: 'none',
        },
        options: {
            arrowColor: '#CFEDFB',
        }
    }
}