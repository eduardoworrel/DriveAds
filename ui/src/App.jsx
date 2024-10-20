import * as React from 'react';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import theme from './theme';
import { useState, useRef, useEffect } from 'react';
import { MinhasConquistas } from './shared/components/minhas-conquistas/MinhasConquistas';
import { Skeleton, TextField } from '@mui/material';
function getSupportedMimeType(types) {
  return types.find((type) => MediaRecorder.isTypeSupported(type)) || null;
}
export default function App() {
  const [media, setMedia] = useState(null);
  const [ative, setAtive] = useState(false);
  const [nome, setNome] = useState('');
  const [renderSendName, setRenderSendName] = useState(false);
  const videoRef = useRef(null);
  const wsRef = useRef(null);

  const constraints = {
    audio: false,
    video: true,
  };

  const handleChangeNome = (e) => {
    if (e && e.target) {
      const txt = e.target.value;
      setNome(txt);
    } else {
      console.error("Evento inválido ou 'target' indefinido.");
    }
  };

  const handleSendNome = () => {
    if(nome.length == 0){
      return alert('The name field is mandatory.')
    }
    setRenderSendName(true); // Altera o estado para mostrar o restante da página
  };

  const handleVideo = async () => {
    if (ative) {
      if (media) {
        media.getTracks().forEach(track => track.stop());
      }
      setAtive(false);
      setMedia(null);
      if (wsRef.current) {
        wsRef.current.close();
      }
      console.log("Media stopped and WebSocket closed");
    } else {
      try {
        const stream = await navigator.mediaDevices.getUserMedia(constraints);
        setMedia(stream);
        setAtive(true);
        console.log("Media started");

        wsRef.current = new WebSocket('wss://ws.eduardoworrel.com/api/sync/'+nome + (new Date()).toTimeString());
        wsRef.current.onopen = () => {
          console.log('WebSocket connected');
          sendMediaStream(stream);
        }
        wsRef.current.onclose = () => {
          console.log('WebSocket closed');
        };
      } catch (error) {
        console.error(`getUserMedia error: ${error.name}`, error);
      }
    }
  };
  let types = ['video/webm;codecs=h264',
    'video/webm',
    'video/mp4'];
let mimeType = getSupportedMimeType(types);
  const sendMediaStream = (stream) => {
    const mediaRecorder = new MediaRecorder(stream,{
        mimeType:mimeType
    });

    wsRef.current.onmessage = (event) => {
      const message = event.data;
      console.log('Received message:', message);
      if(message == "free"){
        if(mediaRecorder.state != "recording"){
          mediaRecorder.start(3000);
        }
      }else{
        textToSpeech(message);
      }
    };


    const reader = new FileReader();
   
  
    const textToSpeech = (text) => {
      const synth = window.speechSynthesis;
      const utterance = new SpeechSynthesisUtterance(text);
      utterance.pitch = 1;
      utterance.rate = 1;
      synth.speak(utterance);
    };

    mediaRecorder.ondataavailable = (event) => {

      console.log('event')
      if (event.data.size > 0) {
        mediaRecorder.stop();
        const videoBlob = event.data;
        if (wsRef.current.readyState === WebSocket.OPEN) {
          const file = new File([videoBlob], 'filename', { type: 'video/webm' });
          reader.onload = function(e) {
            const rawData = e.target.result;
            wsRef.current.send(rawData);
          };
          try{
            reader.readAsArrayBuffer(file);
          }catch{}
        }
      }
    };

    mediaRecorder.start(3000);
    console.log('start')
    const stopRecording = () => {
      mediaRecorder.stop();
      console.log("Recording stopped");
    };

    return { stopRecording };
  };

  useEffect(() => {
    if (media && videoRef.current) {
      videoRef.current.srcObject = media;
    }
  }, [media]);



  return (
    <Box display="flex" flexDirection={'column'} justifyContent="center" alignItems="center" height="100vh" width="100vw" bgcolor={theme.palette.background.paper}>
      {!renderSendName && (
        <Box display={'flex'} justifyContent={'center'} flexDirection={'column'} width={theme.spacing(50)}  gap={10}>
          <Box justifyContent={'center'} display={'flex'}>
            <TextField
              label={'Name:'}
              onChange={handleChangeNome}
            />
          </Box>
          <Box justifyContent={'center'} display={'flex'} >
            <Button onClick={handleSendNome} style={{ backgroundColor: theme.palette.primary.dark, width:'100px' }}  >
              Send
            </Button>
          </Box>
        </Box>
      )}

      {renderSendName && (
        <>

          <Box display="flex" flexDirection="column">
          
              <Button onClick={handleVideo} style={{ backgroundColor: ative ? theme.palette.primary.dark : theme.palette.primary.dark, width:'100px'  }}>
                {ative ? 'Cancel' : 'Start'}
              </Button>

          </Box>
        </>
      )}
    </Box>
  );
}
