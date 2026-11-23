<script>
	import { slide} from 'svelte/transition'

	let result='';
	let showbuttons = true;
	$:showresult = result;


	async function saveOkOrNot(isok) {
	var res = await fetch ("/sendok.ashx?isok=" + isok);
	result = await res.text();
	document.getElementById("container");
	getlastinfo();
	}

	function hidebuttons(){
	showbuttons = false;
	setTimeout(()=>{showbuttons=true;}, 5000);
	}
	async function ok() {
	hidebuttons();
	await saveOkOrNot(true);
	}
	async function notok() {
	hidebuttons();
	await saveOkOrNot(false);
	}

	let laatsteokmeldingen;
	let nietokmeldingen;
	async function getlastinfo(){
	var res = await fetch ("/getlastok.ashx" );
	laatsteokmeldingen = await res.text();

	var res = await fetch ("/getlastnotok.ashx");
	nietokmeldingen = await res.text();
	}

	getlastinfo();
</script>

<style>
  button {
    width: 200px;
    height: 200px;
    margin: 40px;
    font-weight: bold;
    font-size: 18px;
    color: white;
  }

  .ok {
    background-color: green;
  }

  .notok {
    background-color: red;
  }

  .result{
	  font-size:16px;
	  border:1px solid grey;

  }
</style>

<main>
	{#if showbuttons}
	<div transition:slide>
  <button on:click={ok} class="ok">OK</button>
  <button on:click={notok} class="notok">NOT OK</button>
	</div>
	{:else}
  <div class="result"  >{showresult}</div>
	{/if}

	Laatste OK meldingen:<br/>
	{@html laatsteokmeldingen}
<br/>
<br/>
	Laatste NO OK meldingen:<br/>
	{@html  nietokmeldingen}
</main>
 