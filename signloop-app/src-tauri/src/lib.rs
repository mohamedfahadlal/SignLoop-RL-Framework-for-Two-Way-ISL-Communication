use std::sync::Mutex;
use tauri::{command, State};
use ort::session::Session;
use ort::value::Tensor;

pub struct AppState {
    pub session: Mutex<Option<Session>>,
}

#[command]
fn run_model_inference(state: State<'_, AppState>, coordinates: Vec<f32>) -> String {
    // 1. Verify tensor size (30 frames * 225 dimensions = 6750 floats)
    if coordinates.len() != 30 * 225 {
        return serde_json::to_string(&serde_json::json!({
            "error": "Invalid tensor dimensions",
            "predicted_id": -1,
            "confidence": 0.0
        })).unwrap_or_default();
    }

    let mut session_guard = match state.session.lock() {
        Ok(guard) => guard,
        Err(e) => return serde_json::to_string(&serde_json::json!({
            "error": format!("Mutex Lock Error: {}", e),
            "predicted_id": -1,
            "confidence": 0.0
        })).unwrap_or_default(),
    };

    let session = match session_guard.as_mut() {
        Some(s) => s,
        None => return serde_json::to_string(&serde_json::json!({
            "error": "ONNX Session not loaded",
            "predicted_id": -1,
            "confidence": 0.0
        })).unwrap_or_default(),
    };

    // Pass the shape and vector as a tuple directly to Tensor::from_array
    let tensor_value = match Tensor::from_array(([1_i64, 30, 225], coordinates)) {
        Ok(val) => val,
        Err(_) => return serde_json::to_string(&serde_json::json!({
            "error": "Failed to create ONNX tensor value",
            "predicted_id": -1,
            "confidence": 0.0
        })).unwrap_or_default(),
    };

    let inputs: Vec<(String, ort::session::SessionInputValue)> = vec![
        ("input_state".to_string(), tensor_value.into())
    ];

    let outputs = match session.run(inputs) {
        Ok(out) => out,
        Err(e) => return serde_json::to_string(&serde_json::json!({
            "error": format!("Inference Error: {}", e),
            "predicted_id": -1,
            "confidence": 0.0
        })).unwrap_or_default(),
    };

    let (_, logits_data) = match outputs["logits"].try_extract_tensor::<f32>() {
        Ok(tuple) => tuple,
        Err(_) => return serde_json::to_string(&serde_json::json!({
            "error": "Failed to extract logits",
            "predicted_id": -1,
            "confidence": 0.0
        })).unwrap_or_default(),
    };
    
    // Numerically stable Softmax
    let max_logit = logits_data.iter().cloned().fold(f32::NEG_INFINITY, f32::max);
    let exps: Vec<f32> = logits_data.iter().map(|&x| (x - max_logit).exp()).collect();
    let sum_exp: f32 = exps.iter().sum();

    let mut max_prob = 0.0f32;
    let mut predicted_id = 0;

    for (i, &exp_val) in exps.iter().enumerate() {
        let prob = exp_val / sum_exp;
        if prob > max_prob {
            max_prob = prob;
            predicted_id = i;
        }
    }

    serde_json::to_string(&serde_json::json!({
        "predicted_id": predicted_id,
        "confidence": max_prob
    })).unwrap_or_default()
}

fn find_onnx_model() -> std::path::PathBuf {
    // 1. Check next to the running executable
    if let Ok(exe_path) = std::env::current_exe() {
        if let Some(exe_dir) = exe_path.parent() {
            let next_to_exe = exe_dir.join("isl_policy_10k.onnx");
            if next_to_exe.exists() {
                println!("Loading ONNX model next to exe: {:?}", next_to_exe);
                return next_to_exe;
            }
        }
    }

    // 2. Check candidate working directory paths
    let candidates = [
        "isl_policy_10k.onnx",
        "src-tauri/isl_policy_10k.onnx",
        "../src-tauri/isl_policy_10k.onnx",
        "models/isl_policy_clean.onnx",
        "models/isl_policy_10k.onnx",
        "../models/isl_policy_10k.onnx",
        "../../models/isl_policy_10k.onnx",
    ];

    for c in &candidates {
        let p = std::path::PathBuf::from(c);
        if p.exists() {
            println!("Loading ONNX model from candidate: {:?}", p);
            return p;
        }
    }

    println!("Fallback to isl_policy_10k.onnx in cwd");
    std::path::PathBuf::from("isl_policy_10k.onnx")
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let model_path = find_onnx_model();
    println!("Initializing ONNX session from {:?}", model_path);

    let session = match Session::builder() {
        Ok(mut builder) => match builder.commit_from_file(&model_path) {
            Ok(sess) => {
                println!("Successfully loaded ONNX model from {:?}", model_path);
                Some(sess)
            },
            Err(e) => {
                eprintln!("Warning: Failed to load ONNX model from {:?}: {}", model_path, e);
                None
            }
        },
        Err(e) => {
            eprintln!("Warning: Failed to build ONNX session: {}", e);
            None
        }
    };

    tauri::Builder::default()
        .manage(AppState {
            session: Mutex::new(session),
        })
        .plugin(tauri_plugin_shell::init())
        .invoke_handler(tauri::generate_handler![run_model_inference])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}