import * as React from 'react';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import theme from './theme';
import { useState, useRef, useEffect } from 'react';
import { MinhasConquistas } from './shared/components/minhas-conquistas/MinhasConquistas';
import { Skeleton, TextField } from '@mui/material';
const textToSpeech = (text) => {
  const synth = window.speechSynthesis;
  const utterance = new SpeechSynthesisUtterance(text);
  utterance.pitch = 1;
  utterance.rate = 1;
  synth.speak(utterance);
};

function blobToBuffer(blob) {
  return new Promise((resolve, reject) => {
      const reader = new FileReader();

      reader.onload = function(event) {
          resolve(event.target.result); // Resolvendo com o ArrayBuffer
      };

      reader.onerror = function(err) {
          reject(err); // Rejeitando em caso de erro
      };

      reader.readAsArrayBuffer(blob); // Lê o Blob como ArrayBuffer
  });
}
export default function App() {
  const [media, setMedia] = useState(null);
  const [ative, setAtive] = useState(false);
  const [nome, setNome] = useState('');
  const [renderMinhasConquistas, setRenderMinhasConquistas] = useState(false);
  const [renderSendName, setRenderSendName] = useState(false);
  const videoRef = useRef(null);
  const wsRef = useRef(null);
  const captureIntervalRef = useRef(null);
  const ShouldCaptureIntervalRef = useRef(true);

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
      // Se já está ativo, parar a captura e fechar o WebSocket
      if (media) {
        media.getTracks().forEach(track => track.stop());
      }
      clearInterval(captureIntervalRef.current);
      setAtive(false);
      setMedia(null);
      if (wsRef.current) {
        wsRef.current.close();
      }
      console.log("Media stopped and WebSocket closed");
    } else {
      // Iniciar a captura de mídia e WebSocket
      try {
        const stream = await navigator.mediaDevices.getUserMedia(constraints);
        setMedia(stream);
        setAtive(true);
        console.log("Media started");

        wsRef.current = new WebSocket('ws://localhost:5017/api/sync/' + nome + (new Date()).toTimeString());
        wsRef.current.onopen = () => {
          console.log('WebSocket connected');
          startImageCapture(stream);
        }
        wsRef.current.onclose = () => {
          console.log('WebSocket closed');
          wsRef.current.close();
        };
      } catch (error) {
        console.error(`getUserMedia error: ${error.name}`, error);
      }
    }
  };

  const startImageCapture = (stream) => {
    wsRef.current.onmessage = (event) => {
      const message = event.data;
      console.log('Received message:', message);
      if(message == "free"){
        ShouldCaptureIntervalRef.current = true;
      }else{
        textToSpeech(message);
      }
    };
    const track = stream.getVideoTracks()[0];
    const imageCapture = new ImageCapture(track);

    // Configura a captura de imagem em intervalos regulares (por exemplo, a cada 3 segundos)
    captureIntervalRef.current = setInterval(async () => {
      if(ShouldCaptureIntervalRef.current == false){
        return;
      }
      try {
        const frame = await imageCapture.takePhoto();
      
        const buffer = await blobToBuffer(frame);

        if (wsRef.current.readyState === WebSocket.OPEN) {
          ShouldCaptureIntervalRef.current = true;
          wsRef.current.send(buffer);
        }
      } catch (error) {
        console.error('Image capture failed:', error);
      }
    }, 5000);
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
          </Box>
        </>
      )}
    </Box>
  );
}