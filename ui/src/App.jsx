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
  const [renderMinhasConquistas, setRenderMinhasConquistas] = useState(false);
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

        wsRef.current = new WebSocket('ws://localhost:5017/api/sync/'+nome + (new Date()).toTimeString());
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

  const handleMinhasConquistas = () => {
    setRenderMinhasConquistas(prev => !prev);
  };

  return (
    <Box display="flex" flexDirection={'column'} justifyContent="center" alignItems="center" height="100vh" width="100vw" bgcolor={theme.palette.background.paper}>
      {!renderSendName && (
        <Box display={'flex'} justifyContent={'space-between'} width={theme.spacing(50)}>
          <Box>
            <TextField
              label={'Nome do motorista: '}
              onChange={handleChangeNome}
            />
          </Box>
          <Button onClick={handleSendNome} style={{backgroundColor:theme.palette.primary.dark}}> 
            Send
          </Button>
        </Box>
      )}

      {renderSendName && (
        <>
          <Skeleton width={theme.spacing(100)} height={2} />

          <Box height={theme.spacing(40)} width={theme.spacing(40)} display={'flex'} alignItems={'center'} justifyContent={'center'} borderColor={theme.palette.primary.dark}>
            {renderMinhasConquistas && (
              <MinhasConquistas />
            )}
          </Box>

          <Box display="flex" borderRadius={5} padding={15} flexDirection="column" gap={10} width={theme.spacing(40)}>
            {!renderMinhasConquistas && (
              <Button onClick={handleVideo} style={{backgroundColor: ative ? theme.palette.primary.dark : theme.palette.primary.light}}>
                {ative ? 'Cancel' : 'Start'}
              </Button>
            )}
            {/* <Button bgcolor={theme.palette.secondary.main} onClick={handleMinhasConquistas} style={{backgroundColor:theme.palette.primary.dark}}>
              {renderMinhasConquistas ? 'Back' : 'Minhas Conquistas'}
            </Button> */}
          </Box>
        </>
      )}
    </Box>
  );
}
