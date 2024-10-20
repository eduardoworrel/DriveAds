import { createTheme } from '@mui/material/styles';
import { red } from '@mui/material/colors';

// Create a theme instance.
const theme = createTheme({
  cssVariables: true,
  palette:{
    primary:{
        main:'#BFACE2',
        dark:'#2E073F',
        light:'#F0F3FF',
        contrastText:'#ffffff',
    },
    secondary:{
        main:'#E5D9F2',
        dark:'#CDC1FF',
        light:'#F5EFFF',
        contrastText:'#ffffff',
    },
    background:{
        default:"#F0F3FF",
        paper:"#ffffff"
    }
}
});

export default theme;
